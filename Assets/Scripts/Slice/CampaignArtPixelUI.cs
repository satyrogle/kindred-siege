using System;
using System.Collections.Generic;
using System.Text;
using KindredSiege.Sim.Learning;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        // The 2x pixel kit (Jacob, 11 Oct 2026: lock 2x, then a PixelLab UI kit in obsidian, ancient gold
        // and bone). Every piece is drawn at 1x on a 720x450 grid and shown at exactly 2x, so it shares one
        // pixel size with the canyon and the fighters; text uses the game's own pixel font. Every page but
        // the title is built with it: the fight in CampaignArtBattle.cs, the others in CampaignArtPixelPages.cs.
        const int Px=2;
        static readonly Color32 PxBone=new Color32(239,223,190,255),PxMuted=new Color32(173,154,134,255),PxMint=new Color32(153,190,172,255),
            PxRed=new Color32(218,107,81,255),PxGold=new Color32(214,170,92,255),PxBlue=new Color32(141,182,224,255),
            PxOrange=new Color32(238,154,77,255),PxLilac=new Color32(194,176,214,255),PxYellow=new Color32(242,210,124,255);
        // The tick for a fighter picked for the squad, seven rows like the font's capitals.
        static readonly string[] PxCheck={".......","......#",".....##","#...##.","##.##..",".###...","..#...."};
        Texture2D _kitButton,_kitPanel;
        readonly Dictionary<string,Texture2D> _kitIcons=new Dictionary<string,Texture2D>();
        readonly Dictionary<string,Texture2D> _kitPlates=new Dictionary<string,Texture2D>();
        readonly Dictionary<string,PxText> _pxButtonLabels=new Dictionary<string,PxText>();
        // Text and pictures made for the screen on show; SetPage frees them.
        readonly List<Texture2D> _screenTextures=new List<Texture2D>();
        // The notice line at the foot of a pixel page; Update keeps it current.
        PxText _pxNotice;

        /// A line of pixel text made of coloured parts, redrawn only when it changes. align: 0 left,
        /// 1 centred, 2 right, around x. Coordinates are 1x grid pixels.
        sealed class PxText{public RectTransform root;public int x,y,align;public string key="";public float alpha=1;}

        /// Lines of pixel text, each in its own colour; an empty line is a gap.
        sealed class PxLines
        {
            public readonly List<string> texts=new List<string>();
            public readonly List<Color32> colours=new List<Color32>();
            public PxLines Add(string text,Color32 colour){texts.Add(text);colours.Add(colour);return this;}
            public PxLines Gap(){return Add("",PxBone);}
        }

        void LoadPixelKit()
        {
            if(_kitButton!=null)return;
            LoadTitleArt();
            _kitButton=TitleArt("CampaignArt/UI/ui_button",false);
            _kitPanel=TitleArt("CampaignArt/UI/ui_panel",false);
            foreach(string id in new[]{"hold","fallback","focus","unleash","eclipse"})_kitIcons[id]=TitleArt("CampaignArt/UI/icon_"+id,false);
        }

        void FreeScreenTextures()
        {
            foreach(var t in _screenTextures)if(t!=null)Destroy(t);
            _screenTextures.Clear();
            _pxButtonLabels.Clear();
            _pxNotice=null;
        }

        /// A plate of any size from a kit piece: corners kept, edges and middle repeated, never stretched.
        Texture2D Plate(Texture2D source,int border,int w,int h)
        {
            string key=source.GetInstanceID()+":"+border+":"+w+":"+h;
            if(_kitPlates.TryGetValue(key,out Texture2D cached))return cached;
            Color32[] src=source.GetPixels32();int sw=source.width,sh=source.height,midW=sw-2*border,midH=sh-2*border;
            var pixels=new Color32[w*h];
            for(int y=0;y<h;y++)
            {
                int sy=y<border?y:y>=h-border?sh-(h-y):border+(y-border)%midH;
                for(int x=0;x<w;x++)
                {
                    int sx=x<border?x:x>=w-border?sw-(w-x):border+(x-border)%midW;
                    pixels[y*w+x]=src[sy*sw+sx];
                }
            }
            var plate=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            plate.SetPixels32(pixels);plate.Apply();
            _kitPlates[key]=plate;
            return plate;
        }

        RawImage PxImage(Transform parent,string name,Texture2D texture,int x,int y)
        {
            var image=Raw(parent,name,x*Px,y*Px,texture.width*Px,texture.height*Px);
            image.texture=texture;
            return image;
        }

        PxText PxLabel(Transform parent,int x,int y,int align=0)
        {
            return new PxText{root=Rect(parent,"Pixel text",0,0,1,1),x=x,y=y,align=align};
        }

        void SetPx(PxText t,string text,Color32 colour){SetPx(t,new[]{text},new[]{colour});}

        void SetPx(PxText t,string[] texts,Color32[] colours)
        {
            // Transitional presenters may still flush a receipt after their screen was destroyed.
            if(t==null||t.root==null)return;
            var key=new StringBuilder();
            for(int i=0;i<texts.Length;i++)key.Append(texts[i]).Append('|').Append(colours[i].r).Append(colours[i].g).Append(colours[i].b).Append('/');
            string k=key.ToString();
            if(k==t.key)return;
            t.key=k;
            for(int i=t.root.childCount-1;i>=0;i--)
            {
                var child=t.root.GetChild(i);var raw=child.GetComponent<RawImage>();
                if(raw!=null&&raw.texture!=null){_screenTextures.Remove((Texture2D)raw.texture);Destroy(raw.texture);}
                Destroy(child.gameObject);
            }
            // A "\n" part starts a new line 10 pixels under the last, each line placed on its own (two line tags,
            // Claude's playtest, 14 Oct 2026).
            var parts=new List<Texture2D>();var lineOf=new List<int>();int line=0;
            for(int i=0;i<texts.Length;i++)
            {
                if(texts[i]=="\n"){line++;continue;}
                if(string.IsNullOrEmpty(texts[i]))continue;
                var texture=_tinyFont.Render(texts[i].ToUpperInvariant(),colours[i],colours[i],1,true);
                _screenTextures.Add(texture);
                parts.Add(texture);lineOf.Add(line);
            }
            for(int l=0;l<=line;l++)
            {
                int width=0,count=0;
                for(int p=0;p<parts.Count;p++)if(lineOf[p]==l){width+=(count>0?6:0)+parts[p].width;count++;}
                int pen=t.align==0?t.x:t.align==1?t.x-width/2:t.x-width;
                for(int p=0;p<parts.Count;p++)
                {
                    if(lineOf[p]!=l)continue;
                    var image=Raw(t.root,"part",pen*Px,(t.y+l*10)*Px,parts[p].width*Px,parts[p].height*Px);
                    image.texture=parts[p];image.color=new Color(1,1,1,t.alpha);
                    pen+=parts[p].width+6;
                }
            }
        }

        /// A plate button from the kit. Its id is what the validators click.
        Button PxButton(string id,string label,int x,int y,int w,int h,Action action,Transform parent=null)
        {
            LoadPixelKit();
            var image=Raw(parent==null?_screen:parent,id,x*Px,y*Px,w*Px,h*Px);
            image.texture=Plate(_kitButton,12,w,h);image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colours=button.colors;colours.normalColor=Color.white;colours.highlightedColor=new Color(1f,.9f,.75f);
            colours.selectedColor=new Color(1f,.8f,.45f);
            colours.pressedColor=new Color(.85f,.7f,.55f);colours.disabledColor=new Color(.5f,.5f,.55f,.85f);button.colors=colours;
            button.onClick.AddListener(()=>action());
            var text=PxLabel(image.transform,w/2,(h-10)/2,1);
            SetPx(text,label,PxBone);
            _pxButtonLabels[id]=text;
            button.name=id;_buttons[id]=button;
            return button;
        }

        /// Pointing at something shows its line in the fight's banner.
        void OnHover(GameObject target,Func<string> line)
        {
            var trigger=target.AddComponent<EventTrigger>();
            var enter=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};enter.callback.AddListener(_=>_hover=line());trigger.triggers.Add(enter);
            var exit=new EventTrigger.Entry{eventID=EventTriggerType.PointerExit};exit.callback.AddListener(_=>_hover="");trigger.triggers.Add(exit);
            var select=new EventTrigger.Entry{eventID=EventTriggerType.Select};select.callback.AddListener(_=>KeyboardHint(target,line()));trigger.triggers.Add(select);
            var deselect=new EventTrigger.Entry{eventID=EventTriggerType.Deselect};deselect.callback.AddListener(_=>_keyboardHint="");trigger.triggers.Add(deselect);
        }
        string _hover="";

        /// Lines of pixel text wrapped to max, from (x,y) down, 11 pixels apart. Returns the y under them.
        int PxWrite(Transform parent,int x,int y,int max,PxLines lines)
        {
            for(int i=0;i<lines.texts.Count;i++)
                foreach(string part in _tinyFont.Wrap(lines.texts[i].ToUpperInvariant(),max,1))
                {
                    if(part.Length>0)SetPx(PxLabel(parent,x,y),part,lines.colours[i]);
                    y+=11;
                }
            return y;
        }

        int PxLineCount(PxLines lines,int max)
        {
            int count=0;
            for(int i=0;i<lines.texts.Count;i++)count+=_tinyFont.Wrap(lines.texts[i].ToUpperInvariant(),max,1).Count;
            return count;
        }

        /// The page title in the display font at (14,4), then lines under it wrapped to the page. Returns
        /// the y under the last line.
        int PxHeader(string title,Color32 fill,Color32 lower,PxLines lines)
        {
            LoadPixelKit();
            var texture=_logoFont.Render(title.ToUpperInvariant(),fill,lower,1,true);_screenTextures.Add(texture);
            PxImage(_screen,"Title",texture,14,4);
            return PxWrite(_screen,16,38,686,lines);
        }

        /// A framed panel of wrapped text; capitals only lines are headings, drawn in gold.
        void PxPanelText(int x,int y,int w,int h,string text)
        {
            PxImage(_screen,"Panel",Plate(_kitPanel,46,w,h),x,y);
            int line=y+26,max=w-52;
            foreach(string raw in text.Split('\n'))
            {
                bool heading=raw.Length>0&&raw==raw.ToUpperInvariant()&&raw.ToLowerInvariant()!=raw;
                foreach(string part in _tinyFont.Wrap(raw.ToUpperInvariant(),max,1))
                {
                    if(line>y+h-24)return;
                    if(part.Length>0)SetPx(PxLabel(_screen,x+26,line),part,heading?PxGold:PxBone);
                    line+=11;
                }
            }
        }

        /// A framed panel of coloured lines.
        void PxPanel(int x,int y,int w,int h,PxLines lines,Transform parent=null){PxPanel(x,y,w,h,new[]{lines},parent);}

        /// A framed panel that shows the first of its versions, fullest first, that fits the frame whole.
        /// When none fits, the last one scrolls inside the frame, a line per wheel step, kept on the 2x
        /// grid. Returns false when it had to scroll.
        bool PxPanel(int x,int y,int w,int h,PxLines[] versions,Transform parent=null)
        {
            LoadPixelKit();
            if(parent==null)parent=_screen;
            PxImage(parent,"Panel",Plate(_kitPanel,46,w,h),x,y);
            int view=(h-50)/11*11,max=w-52; // whole lines only, so none is cut in half at the bottom
            foreach(var lines in versions)
                if(PxLineCount(lines,max)*11<=view){PxWrite(parent,x+26,y+26,max,lines);return true;}
            var last=versions[versions.Length-1];
            max-=8; // room for the scroll bar
            PxWrite(PxScroll(parent,x+26,y+26,w-52,view,PxLineCount(last,max)*11,x+w-30),0,0,max,last);
            return false;
        }

        /// Text with line breaks as pixel lines: capitals only lines are headings in gold, the rest bone.
        /// Runs of empty lines count as one gap.
        static PxLines PxTextLines(string text)
        {
            var lines=new PxLines();
            bool gap=true;
            foreach(string raw in text.Split('\n'))
            {
                bool empty=raw.Trim().Length==0;
                if(empty&&gap)continue;
                gap=empty;
                bool heading=!empty&&raw==raw.ToUpperInvariant()&&raw.ToLowerInvariant()!=raw;
                lines.Add(empty?"":raw,heading?PxGold:PxBone);
            }
            return lines;
        }

        /// Frees the text a popup drew when it closes; the kit's plates and icons and any art it showed stay.
        void FreeModalTextures(RectTransform modal)
        {
            foreach(var image in modal.GetComponentsInChildren<RawImage>(true))
            {
                var texture=image.texture as Texture2D;
                if(texture!=null&&_screenTextures.Remove(texture))Destroy(texture);
            }
        }

        RectTransform PxScroll(Transform parent,int x,int y,int w,int h,int contentHeight,int barX)
        {
            var view=Rect(parent,"Scrolling text",x*Px,y*Px,w*Px,h*Px);
            view.gameObject.AddComponent<Image>().color=Color.clear; // catches the mouse wheel
            view.gameObject.AddComponent<RectMask2D>();
            var content=Rect(view,"Lines",0,0,w*Px,contentHeight*Px);
            var scroll=view.gameObject.AddComponent<ScrollRect>();
            scroll.viewport=view;scroll.content=content;scroll.horizontal=false;scroll.inertia=false;
            scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=11*Px;
            scroll.onValueChanged.AddListener(_=>
            {
                Vector2 p=content.anchoredPosition;float snapped=Mathf.Round(p.y/Px)*Px;
                if(snapped!=p.y)content.anchoredPosition=new Vector2(p.x,snapped);
            });
            // The bar belongs to whatever holds the panel, so a popup takes its bar with it when it closes.
            var track=Panel(parent,barX*Px,y*Px,2*Px,h*Px,new Color32(58,48,44,255));track.raycastTarget=true;
            var bar=track.gameObject.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;
            var handle=Panel(track.transform,0,0,0,0,PxGold);
            bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;
            scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.Permanent;
            return content;
        }

        /// A small mark drawn the way the pixel font draws letters: a one pixel dark ring and a drop shadow.
        static Texture2D PxGlyph(string[] rows,Color32 colour)
        {
            int gw=rows[0].Length,gh=rows.Length,w=gw+2,h=gh+3;
            var ink=new bool[w*h];
            for(int y=0;y<gh;y++)for(int x=0;x<gw;x++)if(rows[y][x]=='#')ink[(y+1)*w+x+1]=true;
            var pixels=new Color32[w*h];
            for(int y=0;y<h;y++)
                for(int x=0;x<w;x++)
                {
                    Color32 c=TitleClear;
                    if(ink[y*w+x])c=colour;
                    else if(InkNear(ink,w,h,x,y)||InkNear(ink,w,h,x,y-1))c=TitleNight;
                    pixels[(h-1-y)*w+x]=c;
                }
            var texture=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply();
            return texture;
        }

        static bool InkNear(bool[] ink,int w,int h,int x,int y)
        {
            for(int dy=-1;dy<=1;dy++)
                for(int dx=-1;dx<=1;dx++)
                {
                    int nx=x+dx,ny=y+dy;
                    if(nx>=0&&ny>=0&&nx<w&&ny<h&&ink[ny*w+nx])return true;
                }
            return false;
        }

        /// The run as a strip from camp to camp, one plate per tribe (13 Oct 2026: each tribe's road is on the road
        /// page, CampaignArtRoad.cs): passed tribes green, the one you are on lit. _pathMapEnd is where it ends.
        void PxPathMap(int y)
        {
            var route=Route();int at=-1;
            if(_campaign.Target!=null)for(int i=0;i<route.Count;i++)if(route[i].Id==_campaign.Target.Id)at=i;
            bool hunt=_campaign.Target!=null&&_campaign.Target.Id==AlphaHunt.Id;
            if(hunt)at=route.Count;
            int x=16;
            PxNode("CAMP",x,y+10,52,at<0?1:2);x+=56;
            for(int i=0;i<route.Count;i++)
            {
                string title=route[i].Name+" tier "+route[i].Tier;
                int w=_tinyFont.Measure(title.ToUpperInvariant(),1)+28;
                PxNode(title,x,y+10,w,i<at?2:i==at?1:0);x+=w+4;
            }
            PxNode("HUNTERS",x,y+10,68,hunt?1:0);x+=72;
            PxNode("CAMP",x,y+10,52,0);
            _pathMapEnd=x+52;
        }

        /// state 0 waits, 1 is next, 2 is won.
        void PxNode(string title,int x,int y,int w,int state)
        {
            var plate=PxImage(_screen,title,Plate(_kitButton,12,w,24),x,y);
            plate.color=state==1?Color.white:state==2?new Color(.72f,.95f,.8f):new Color(.5f,.5f,.55f);
            SetPx(PxLabel(_screen,x+w/2,y+7,1),title,state==2?PxMint:state==1?PxBone:PxMuted);
        }
    }
}
