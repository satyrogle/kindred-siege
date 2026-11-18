import sys, zipfile, io, os
from PIL import Image, ImageDraw
S=sys.argv[1]; A=S+"/allin"; Z=A+"/zips"
R="C:/Users/jacob/KindredSiege-Clean/Assets/StreamingAssets/CampaignArt/Bloodridge/"
bg=Image.open(R+"ground-underpaint.png").convert("RGBA")
for l in ["rear-burial-architecture.png","rooted-entrance.png"]:
    bg.alpha_composite(Image.open(R+l).convert("RGBA"))
def clip(cid, name, d):
    z=zipfile.ZipFile(f"{Z}/{cid}.zip")
    fr=[]
    for i in range(17):
        fr.append(Image.open(io.BytesIO(z.read(f"Idle/animations/{name}/{d}/frame_{i:03d}.png"))).convert("RGBA"))
    return fr
def local(path):
    return [Image.open(f"{path}/{i}.png").convert("RGBA") for i in range(17)]
units=[ # frames, feet x, feet y, phase
 (local(S+"/tank3/t3"), 655,565, 0, "our warden"),
 (clip("a1195c8f-03c0-42f8-8f8f-135b8e98d09c","allin_skirmisher_hack_sw_20261106","south-west"), 745,565, 2, "their skirmisher"),
 (clip("a3aa47e7-da8b-47f5-a04d-dadb14721a65","allin_skirmisher_combo_se_20261106","south-east"), 665,445, 5, "our skirmisher"),
 (clip("b3fef59f-4ba2-4f95-a526-1228e1f8b49d","allin_warden_brace_strike_sw_20261106","south-west"), 755,445, 4, "their warden"),
 (clip("44547cca-99d8-432c-bc41-ab629db97136","allin_trapper_jab_se_20261106","south-east"), 665,690, 9, "our trapper"),
 (clip("720d540c-6f12-49b8-b409-3f84e8102bf2","allin_trapper_slash_sw_20261106","south-west"), 745,690, 11, "their trapper"),
 (clip("9e0233b1-4a8a-4d81-99d8-53351b1cf28d","allin_marksman_shot_se_20261106","south-east"), 480,500, 3, "our marksman"),
 (clip("20b62125-093d-41ca-a5d2-eb2f15d3fd25","allin_medic_heal_se_20261106","south-east"), 470,630, 7, "our medic"),
 (clip("19b7cc53-fb54-45dd-95ee-433ed538dd10","allin_marksman_shot_sw_20261106","south-west"), 945,500, 12, "their marksman"),
 (clip("e7aceef9-d12c-4c53-8817-318de3265c03","allin_medic_heal_sw_20261106","south-west"), 955,630, 14, "their medic"),
]
anch=[]
for fr,x,y,ph,n in units:
    a=fr[0].split()[3].point(lambda v:255 if v>20 else 0).getbbox()
    anch.append(((a[0]+a[2])//2, a[3]))
order=sorted(range(len(units)), key=lambda k: units[k][2])
box=(340,300,1080,760)
out=[]
N=34
for f in range(N):
    c=bg.copy()
    for k in order:
        fr,x,y,ph,n=units[k]; ax,ay=anch[k]
        c.alpha_composite(fr[(f+ph)%17],(x-ax,y-ay))
    cc=c.crop(box)
    out.append(cc.resize((cc.width*2,cc.height*2),Image.NEAREST))
os.makedirs(A+"/out",exist_ok=True)
out[6].convert("RGB").save(A+"/out/still10.png")
pal=[o.convert("P",palette=Image.ADAPTIVE,colors=255) for o in out]
pal[0].save(A+"/out/all-in-fight.gif",save_all=True,append_images=pal[1:],duration=90,loop=0,disposal=2)
print("ok")
