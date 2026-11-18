using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using KindredSiege.Sim.Battle;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    public class CampaignPersistenceTests
    {
        private static Campaign Ready(ulong seed = 42)
        {
            var c = Campaign.New(seed);
            Assert.IsTrue(CampaignPilot.PlayTutorial(c, PilotStyle.Reference, seed, 10));
            return c;
        }

        private static Campaign Active()
        {
            var c = Ready(); c.StartRun(); var f = c.StartEncounter(0);
            var pilot = new CampaignPilot(PilotStyle.Reference, 42);
            while (!f.Over && f.Sim.CurrentTick < 90) { pilot.Step(f); f.Tick(); }
            return c;
        }

        private static void AtPath(Action<string> test)
        {
            string folder = Path.Combine(Path.GetTempPath(), "ks-save-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            // Deliberately do not delete evidence on failure. No owner path is ever used.
            test(Path.Combine(folder, "campaign.txt"));
            foreach (string file in Directory.GetFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
        }

        [Test]
        public void FinishedFightsAndEveryLessonSurviveReloadAndRunReset()
        {
            var c = Ready();
            CampaignPilot.PlayRun(c, CampaignTribes.BloodridgeId, PilotStyle.Reference, 42);
            var read = CampaignSave.Read(CampaignSave.Write(c));
            Assert.Greater(c.Archive.Fights.Count, 1);
            Assert.AreEqual(CampaignSave.Write(c), CampaignSave.Write(read));
            foreach (var fight in c.Archive.Fights)
            {
                Assert.IsTrue(read.Archive.TryReplay(fight.FightId, out var replay, out string why), why);
                Assert.AreEqual(fight.Record.Checksum, StateChecksum.Compute(replay.OrderedStateSnapshot));
                Assert.AreEqual(fight.Record.Events.Count, read.Archive.Find(fight.FightId).Record.Events.Count);
            }
            int lessons = 0;
            foreach (var mind in CampaignTribes.All.Select(t => read.Mind(t.Id)).Concat(new[] { read.CultMind }))
                foreach (var lesson in mind.Lessons)
                {
                    Assert.IsTrue(read.Archive.TryVerifyLesson(lesson, out string why), why); lessons++;
                }
            Assert.Greater(lessons, 0);
        }

        [Test]
        public void EveryTuningFieldInvalidatesActiveResumeWithoutLosingCampOrArchive()
        {
            var c = Active(); string saved = CampaignSave.Write(c);
            foreach (var property in typeof(CampaignTuning).GetProperties().Where(p => p.CanWrite))
            {
                var tuning = c.Tuning; object value = property.GetValue(tuning);
                property.SetValue(tuning, property.PropertyType == typeof(bool) ? (object)!(bool)value :
                    property.PropertyType.IsEnum ? Enum.ToObject(property.PropertyType, ((int)value + 1) % SliceClasses.Count) : (int)value + 1);
                var read = CampaignSave.Read(saved, tuning);
                Assert.IsNull(read.Active, property.Name + " was not covered by the rules identity");
                Assert.AreEqual(CampaignSave.FightLostNote, read.LoadNote);
                Assert.AreEqual(c.Materials, read.Materials);
                Assert.AreEqual(c.Archive.Fights.Count, read.Archive.Fights.Count);
                CollectionAssert.AreEqual(c.Codex, read.Codex);
                Assert.AreEqual(c.Stage, read.Stage);
            }
        }

        [Test]
        public void WrongRulesSetupAndFinalChecksumEachRejectTheFight()
        {
            var c = Active(); string saved = CampaignSave.Write(c);
            string proof = saved.Split('\n').Single(l => l.StartsWith("proof\t", StringComparison.Ordinal));
            for (int field = 1; field <= 3; field++)
            {
                var fields = proof.Split('\t'); fields[field] = "0000000000000000";
                var read = CampaignSave.Read(saved.Replace(proof, string.Join("\t", fields)));
                Assert.IsNull(read.Active); Assert.AreEqual(CampaignSave.FightLostNote, read.LoadNote);
                Assert.AreEqual(c.Materials, read.Materials); Assert.AreEqual(c.Step, read.Step);
                // Rollback must restore seed/counter state, so retrying starts the same encounter.
                Assert.AreEqual(c.Active.Setup.Seed, read.StartEncounter(0).Setup.Seed);
                Assert.AreEqual(c.Active.Setup.FightId, read.Active.Setup.FightId);
            }
        }

        [Test]
        public void SameTickCommandsResumeWithTheirWrapperStateAndNextSequence()
        {
            var c = Ready(); c.StartRun(); var f = c.StartEncounter(0);
            Assert.IsTrue(f.TryOrder(Directive.Hold, "", out _));
            Assert.IsTrue(f.TryOrder(Directive.FocusFire, f.Setup.Side(1)[0].Id, out _));
            var read = CampaignSave.Read(CampaignSave.Write(c));
            Assert.AreEqual(f.Session.Checksum, read.Active.Session.Checksum);
            Assert.AreEqual(f.Session.Log.ToJson(), read.Active.Session.Log.ToJson());
            for (int i = 0; i < 160 && !f.Over; i++) { f.Tick(); read.Active.Tick(); }
            bool a = f.TryOrder(Directive.Unleash, "", out _);
            bool b = read.Active.TryOrder(Directive.Unleash, "", out _);
            Assert.AreEqual(a, b); Assert.AreEqual(f.Session.Log.ToJson(), read.Active.Session.Log.ToJson());
        }

        [Test]
        public void GenuineVersionThreeShapeKeepsCampAndRunButRestartsUnverifiedFight()
        {
            var c = Active();
            string old = string.Join("\n", CampaignSave.Write(c).Split('\n')
                .Where(l => !l.StartsWith("archive\t") && !l.StartsWith("proof\t") && !l.StartsWith("clearroad\t") && !l.StartsWith("palroad\t") && !l.StartsWith("beastroll\t")))
                .Replace(CampaignSave.Header + "\t" + CampaignSave.Version, CampaignSave.Header + "\t3");
            var read = CampaignSave.Read(old);
            Assert.IsNull(read.Active); Assert.AreEqual(c.Stage, read.Stage);
            Assert.AreEqual(c.Materials, read.Materials); Assert.AreEqual(0, read.Archive.Fights.Count);
            Assert.AreEqual(c.CultMind.Snapshot(), read.CultMind.Snapshot());
            Assert.AreEqual(CampaignSave.FightLostNote, read.LoadNote);
            var camp = Ready();
            old = string.Join("\n", CampaignSave.Write(camp).Split('\n').Where(l => !l.StartsWith("archive\t")))
                .Replace(CampaignSave.Header + "\t" + CampaignSave.Version, CampaignSave.Header + "\t3");
            Assert.IsEmpty(CampaignSave.Read(old).LoadNote);
        }

        [Test]
        public void DifferentBattleRulesKeepHistoricalEventsButNeverClaimVerifiedReplay()
        {
            var c = Ready(); var fight = c.Archive.Fights[0];
            string saved = CampaignSave.Write(c);
            string line = saved.Split('\n').First(l => l.StartsWith("archive\t"));
            byte[] bytes = Inflate(line.Substring(8));
            byte[] token = Encoding.UTF8.GetBytes(fight.RulesIdentity);
            int at = Find(bytes, token); Assert.GreaterOrEqual(at, 0);
            bytes[at] = (byte)'X';
            var read = CampaignSave.Read(saved.Replace(line, "archive\t" + Deflate(bytes)));
            Assert.AreEqual(fight.Record.Events.Count, read.Archive.Fights[0].Record.Events.Count);
            Assert.IsFalse(read.Archive.TryReplay(fight.FightId, out _, out string why));
            StringAssert.Contains("different battle rules", why);
            Assert.AreEqual(CampaignSave.Write(read), CampaignSave.Write(CampaignSave.Read(CampaignSave.Write(read))));
        }

        [Test]
        public void BadAndDuplicateArchiveRecordsAreRefusedWithoutPartialCampaign()
        {
            var c = Ready(); string saved = CampaignSave.Write(c);
            string line = saved.Split('\n').First(l => l.StartsWith("archive\t"));
            foreach (string bad in new[] { saved.Replace(line, "archive\tnot base64"), saved.Replace(line, line + "\n" + line),
                saved.Replace(line, "archive\t" + Convert.ToBase64String(new byte[] { 1, 0, 0, 0 })) })
            {
                Assert.IsFalse(CampaignSave.TryRead(bad, null, out var read, out string why));
                Assert.IsNull(read); Assert.IsNotEmpty(why);
            }
        }

        [Test]
        public void SafeWriteKeepsExactlyThePreviousSaveAndReopensAnActiveFight()
        {
            AtPath(path =>
            {
                var first = Ready(); CampaignSave.WriteFile(path, first); string old = File.ReadAllText(path);
                var active = Active(); CampaignSave.WriteFile(path, active);
                Assert.AreEqual(old, File.ReadAllText(path + ".bak"));
                Assert.IsTrue(CampaignSave.TryReadFile(path, null, out var read, out string why), why);
                Assert.AreEqual(CampaignSave.Write(active), CampaignSave.Write(read));
                Assert.IsEmpty(read.LoadNote); Assert.AreEqual(0, Directory.GetFiles(Path.GetDirectoryName(path), "*.tmp").Length);
            });
        }

        [Test]
        public void TruncatedOrBitFlippedPrimaryFallsBackAndNextWritePreservesTheGoodBackup()
        {
            AtPath(path =>
            {
                var c = Ready(); CampaignSave.WriteFile(path, c); CampaignSave.WriteFile(path, Active());
                string backup = File.ReadAllText(path + ".bak"), latest = File.ReadAllText(path);
                foreach (string damaged in new[] { latest.Substring(0, latest.Length - 4),
                    latest.Replace("camp\t" + c.Materials + "\n", "camp\t" + (c.Materials + 1) + "\n"), "garbage" })
                {
                    Assert.IsTrue(latest != damaged, "The corruption fixture must change the primary bytes.");
                    File.WriteAllText(path, damaged);
                    Assert.IsTrue(CampaignSave.TryReadFile(path, null, out var read, out string why), why);
                    StringAssert.Contains(CampaignSave.BackupLoadedNote, read.LoadNote);
                    Assert.AreEqual(c.Materials, read.Materials);
                    CampaignSave.WriteFile(path, read);
                    Assert.AreEqual(backup, File.ReadAllText(path + ".bak"));
                }
            });
        }

        [Test]
        public void MissingPrimaryUsesBackupButOrphanTempIsNeverCommitted()
        {
            AtPath(path =>
            {
                var c = Ready(); CampaignSave.WriteFile(path, c); CampaignSave.WriteFile(path, Active());
                File.Delete(path); File.WriteAllText(path + ".orphan.tmp", CampaignSave.Write(Active()));
                Assert.IsTrue(CampaignSave.TryReadFile(path, null, out var read, out _));
                Assert.IsNull(read.Active); Assert.AreEqual(c.Materials, read.Materials);
                File.Delete(path + ".bak");
                Assert.IsFalse(CampaignSave.TryReadFile(path, null, out read, out _)); Assert.IsNull(read);
            });
        }

        [Test]
        public void ReplacementFailureKeepsPrimaryAndBackupBytes()
        {
            AtPath(path =>
            {
                CampaignSave.WriteFile(path, Ready()); CampaignSave.WriteFile(path, Active());
                string primary = File.ReadAllText(path), backup = File.ReadAllText(path + ".bak");
                // Windows target: reader denies delete-sharing, so Replace must fail after the temp was written.
                using (var held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    Assert.Throws<IOException>(() => CampaignSave.WriteFile(path, Ready(43)));
                Assert.AreEqual(primary, File.ReadAllText(path)); Assert.AreEqual(backup, File.ReadAllText(path + ".bak"));
                Assert.AreEqual(0, Directory.GetFiles(Path.GetDirectoryName(path), "*.tmp").Length);
            });
        }

        [Test]
        public void WriterLockAndFutureVersionRefuseToOverwrite()
        {
            AtPath(path =>
            {
                CampaignSave.WriteFile(path, Ready()); string original = File.ReadAllText(path);
                using (var held = new FileStream(path + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    Assert.Throws<IOException>(() => CampaignSave.WriteFile(path, Active()));
                Assert.AreEqual(original, File.ReadAllText(path));
                string future = CampaignSave.Write(Active()).Replace(CampaignSave.Header + "\t" + CampaignSave.Version, CampaignSave.Header + "\t999");
                File.WriteAllText(path + ".bak", original); File.WriteAllText(path, future);
                Assert.IsFalse(CampaignSave.TryReadFile(path, null, out _, out string why)); StringAssert.Contains("newer", why);
                Assert.Throws<IOException>(() => CampaignSave.WriteFile(path, Ready()));
                Assert.AreEqual(future, File.ReadAllText(path)); Assert.AreEqual(original, File.ReadAllText(path + ".bak"));
            });
        }

        [Test]
        public void UnreadablePrimaryAndBackupReturnFailureWithoutRewritingAnything()
        {
            AtPath(path =>
            {
                File.WriteAllText(path, "broken"); File.WriteAllText(path + ".bak", "also broken");
                Assert.IsFalse(CampaignSave.TryReadFile(path, null, out var read, out string why));
                Assert.IsNull(read); Assert.IsNotEmpty(why);
                Assert.AreEqual("broken", File.ReadAllText(path)); Assert.AreEqual("also broken", File.ReadAllText(path + ".bak"));
            });
        }

        private static int Find(byte[] bytes, byte[] token)
        {
            for (int i = 0; i <= bytes.Length - token.Length; i++)
                if (bytes.Skip(i).Take(token.Length).SequenceEqual(token)) return i;
            return -1;
        }

        [Test]
        public void ChangedEventHistoryFailsVerificationEvenWhenFinalChecksumWasNotChanged()
        {
            // A road fight with a blocked Unleash in its history: since 13 Oct 2026 ("fix all 9") the tutorial is practice
            // and nobody Holds against you in it, so the first blocked event comes later. Each fight has its own line.
            var c = Ready(); c.StartRun();
            for (int n = 0; n < 6 && c.Stage == CampaignStage.InRun && !c.Archive.Fights.Any(f => f.Record.Events.Any(e => e.Detail == "blocked")); n++)
                CampaignPilot.PlayFight(c, c.StartEncounter(0), new CampaignPilot(PilotStyle.Reference, 42UL + (ulong)n));
            var fight = c.Archive.Fights.First(f => f.Record.Events.Any(e => e.Detail == "blocked"));
            int index = c.Archive.Fights.ToList().IndexOf(fight);
            string saved = CampaignSave.Write(c), line = saved.Split('\n').Where(l => l.StartsWith("archive\t")).ElementAt(index);
            byte[] bytes = Inflate(line.Substring(8));
            int at = Find(bytes, Encoding.UTF8.GetBytes("blocked")); Assert.GreaterOrEqual(at, 0);
            bytes[at + 2] = (byte)'0';
            var read = CampaignSave.Read(saved.Replace(line, "archive\t" + Deflate(bytes)));
            Assert.AreEqual(fight.Record.Checksum, read.Archive.Fights[index].Record.Checksum);
            Assert.IsFalse(read.Archive.TryReplay(fight.FightId, out _, out string why));
            StringAssert.Contains("history differs", why);
            Assert.IsFalse(FightPlayback.TryOpen(read.Archive, fight.FightId, out var playback, out why));
            Assert.IsNull(playback);
            StringAssert.Contains("history is kept", why);
        }

        [Test]
        public void MissingOrRepeatedCoreCampRecordsNeverSilentlyResetMaterialsOrCounters()
        {
            string saved = CampaignSave.Write(Ready());
            foreach (string name in new[] { "seed", "streams", "counters", "camp" })
            {
                string line = saved.Split('\n').Single(l => l.StartsWith(name + "\t"));
                foreach (string bad in new[] { saved.Replace(line + "\n", ""), saved.Replace(line, line + "\n" + line) })
                {
                    Assert.IsFalse(CampaignSave.TryRead(bad, null, out var read, out string why), name);
                    Assert.IsNull(read); Assert.IsNotEmpty(why);
                }
            }
        }

        [Test]
        public void ExcessiveSavedTickAndMissingProofRollBackPromptly()
        {
            var c = Active(); string saved = CampaignSave.Write(c);
            string fight = saved.Split('\n').Single(l => l.StartsWith("fight\t"));
            var fields = fight.Split('\t'); fields[2] = long.MaxValue.ToString();
            string proof = saved.Split('\n').Single(l => l.StartsWith("proof\t"));
            foreach (string bad in new[] { saved.Replace(fight, string.Join("\t", fields)), saved.Replace(proof + "\n", "") })
            {
                var read = CampaignSave.Read(bad);
                Assert.IsNull(read.Active); Assert.AreEqual(CampaignSave.FightLostNote, read.LoadNote);
                Assert.AreEqual(c.Materials, read.Materials);
            }
        }

        private static byte[] Inflate(string text)
        {
            using (var input = new MemoryStream(Convert.FromBase64String(text)))
            using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream()) { deflate.CopyTo(output); return output.ToArray(); }
        }
        private static string Deflate(byte[] bytes)
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new DeflateStream(output, CompressionLevel.Fastest, true)) deflate.Write(bytes, 0, bytes.Length);
                return Convert.ToBase64String(output.ToArray());
            }
        }
    }
}
