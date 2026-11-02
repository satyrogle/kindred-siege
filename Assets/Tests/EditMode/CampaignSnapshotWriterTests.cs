using System;
using System.IO;
using KindredSiege.Sim.Learning;
using NUnit.Framework;

namespace KindredSiege.Sim.Tests
{
    public class CampaignSnapshotWriterTests
    {
        [Test]
        public void CapturedSaveCannotObserveLaterCampaignOrTuningChanges()
        {
            string path=Path.Combine(Path.GetTempPath(),"ks-snapshot-"+Guid.NewGuid().ToString("N"),"save.txt");
            var campaign=Campaign.New(71);
            string before=CampaignSave.Write(campaign);
            var snapshot=CampaignSave.CaptureFile(campaign);
            campaign.Tuning.RulesCap=14;
            CampaignPilot.PlayTutorial(campaign,PilotStyle.Reference,71,10);
            Assert.AreNotEqual(before,CampaignSave.Write(campaign));
            CampaignSave.WriteFileSnapshot(path,snapshot);
            Assert.IsTrue(CampaignSave.TryReadFile(path,null,out var loaded,out var why),why);
            Assert.AreEqual(before,CampaignSave.Write(loaded));
        }

        [Test]
        public void FlushKeepsLatestSnapshotAndPreservesPathOrderAndBackup()
        {
            string folder=Path.Combine(Path.GetTempPath(),"ks-queue-"+Guid.NewGuid().ToString("N"));
            string a=Path.Combine(folder,"a.txt"),b=Path.Combine(folder,"b.txt");
            using(var writer=new CampaignSave.FileWriter())
            {
                CampaignSave.WriteFile(a,Campaign.New(1));
                for(ulong seed=2;seed<=24;seed++)writer.Submit(a,CampaignSave.CaptureFile(Campaign.New(seed)));
                writer.Submit(b,CampaignSave.CaptureFile(Campaign.New(25)));
                writer.Submit(a,CampaignSave.CaptureFile(Campaign.New(26)));
                writer.Flush();
                Assert.IsNull(writer.TakeError());
                Assert.IsTrue(CampaignSave.TryReadFile(a,null,out var last,out var why),why);
                Assert.AreEqual(CampaignSave.Write(Campaign.New(26)),CampaignSave.Write(last));
                Assert.IsTrue(CampaignSave.TryReadFile(b,null,out var other,out why),why);
                Assert.AreEqual(CampaignSave.Write(Campaign.New(25)),CampaignSave.Write(other));
                File.WriteAllText(a,"damaged");
                Assert.IsTrue(CampaignSave.TryReadFile(a,null,out var backup,out why),why);
                Assert.IsTrue(backup.LoadNote.Contains(CampaignSave.BackupLoadedNote));
            }
        }

        [Test]
        public void FailedWorkerWriteIsReportedAndDoesNotPoisonLaterWrites()
        {
            string path=Path.Combine(Path.GetTempPath(),"ks-queue-failure-"+Guid.NewGuid().ToString("N"),"save.txt");
            CampaignSave.WriteFile(path,Campaign.New(31));
            string before=File.ReadAllText(path);
            using(var writer=new CampaignSave.FileWriter())
            {
                using(var guard=new FileStream(path+".lock",FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                {
                    writer.Submit(path,CampaignSave.CaptureFile(Campaign.New(32)));writer.Flush();
                }
                Assert.IsNotEmpty(writer.TakeError());
                Assert.AreEqual(before,File.ReadAllText(path));
                writer.Submit(path,CampaignSave.CaptureFile(Campaign.New(33)));writer.Flush();
                Assert.IsNull(writer.TakeError());
                Assert.IsTrue(CampaignSave.TryReadFile(path,null,out var loaded,out var why),why);
                Assert.AreEqual(CampaignSave.Write(Campaign.New(33)),CampaignSave.Write(loaded));
            }
        }
    }
}
