using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KindredSiege.Slice
{
    public sealed partial class CampaignArtLoop
    {
        GameObject _modalPreviousSelection;
        string _keyboardHint="";
        float _keyboardHintUntil;

        void KeyboardHint(GameObject control,string text)
        {
            _keyboardHint=text;_keyboardHintUntil=Time.unscaledTime+3f;
            if(control.name.StartsWith("target-",System.StringComparison.Ordinal))_selected=control.name.Substring(7);
        }

        string TargetPrompt()
        {
            var selected=EventSystem.current.currentSelectedGameObject;
            var unit=selected!=null&&selected.name.StartsWith("target-",System.StringComparison.Ordinal)?_fight.Sim.FindById(selected.name.Substring(7)):null;
            return unit!=null?"Target: "+UnitName(unit)+". Tab changes; Enter confirms; Esc cancels.":"Focus Fire: click a living enemy, or Esc to cancel.";
        }

        // Pointer dispatch stays in Unity. Keyboard dispatch belongs here so Space pauses a fight
        // without also submitting the button last clicked, and navigation cannot leave a popup.
        void KeyboardInput()
        {
            if(Input.GetKeyDown(KeyCode.M)){ToggleMute();if(_modal!=null&&_modal.name=="SOUND")ShowSound();}
            if(Input.GetKeyDown(KeyCode.F8)&&_titleCard==null){ShowSound();return;}
            if(_titleCard!=null)return;
            // One dispatcher owns each key press. Starting TitleLife while submitting MENU
            // must not let the new title consume that same Enter again.
            if(_page==Page.Title&&_modal==null)
            {
                if(Input.GetKeyDown(KeyCode.DownArrow)||Input.GetKeyDown(KeyCode.S))HandleUiKey(KeyCode.Tab);
                else if(Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.W))HandleUiKey(KeyCode.Tab,true);
                else if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))HandleUiKey(KeyCode.Return);
                else if(Input.GetKeyDown(KeyCode.Space))HandleUiKey(KeyCode.Space);
                return;
            }
            if(Input.GetKeyDown(KeyCode.Escape))HandleUiKey(KeyCode.Escape);
            else if(Input.GetKeyDown(KeyCode.Tab))HandleUiKey(KeyCode.Tab,Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
            else if(Input.GetKeyDown(KeyCode.DownArrow)||Input.GetKeyDown(KeyCode.RightArrow))HandleUiKey(KeyCode.Tab);
            else if(Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.LeftArrow))HandleUiKey(KeyCode.Tab,true);
            else if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))HandleUiKey(KeyCode.Return);
            else if(Input.GetKeyDown(KeyCode.Space))HandleUiKey(KeyCode.Space);
            if(_modal==null&&_page==Page.Fight)
                for(int i=0;i<Orders.Length;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i))Order(Orders[i]);
        }

        List<Button> KeyboardButtons()
        {
            var buttons=new List<Button>();Transform scope=_modal!=null?_modal:_screen;
            if(scope==null)return buttons;
            foreach(var b in scope.GetComponentsInChildren<Button>())
            {
                if(!b.isActiveAndEnabled||!b.IsInteractable())continue;
                bool target=b.name.StartsWith("target-",System.StringComparison.Ordinal);
                if(_modal==null&&_page==Page.Fight)
                {
                    if(_targeting)
                    {
                        if(!target)continue;
                        var unit=_fight.Sim.FindById(b.name.Substring(7));
                        if(unit==null||!unit.Alive||unit.Side!=1||_fight.Sim.LeaderProtected(unit))continue;
                    }
                    else if(target)continue;
                }
                buttons.Add(b);
            }
            return buttons;
        }

        void HandleUiKey(KeyCode key,bool backwards=false)
        {
            if(_page==Page.Title&&_modal==null)
            {
                if(key==KeyCode.Tab)StepTitle(backwards?-1:1);
                else if((key==KeyCode.Return||key==KeyCode.Space)&&_titleSelected>=0)
                    _titleItems[_titleSelected].Button.onClick.Invoke();
                return;
            }
            if(key==KeyCode.Escape)
            {
                if(_campOpen)CloseCamp();
                else if(_modal!=null)CloseModal();
                else if(_targeting){_targeting=false;SelectControl("order-2");}
                else if(_page==Page.Playback)LeavePlayback();
                return;
            }
            if(key==KeyCode.Space&&(_page==Page.Fight||_page==Page.Playback)&&_modal==null)
            {
                _paused=!_paused;
                return;
            }
            var buttons=KeyboardButtons();if(buttons.Count==0)return;
            var current=EventSystem.current.currentSelectedGameObject;
            int at=buttons.FindIndex(b=>b.gameObject==current);
            if(key==KeyCode.Tab)
            {
                at=at<0?(backwards?buttons.Count-1:0):(at+(backwards?-1:1)+buttons.Count)%buttons.Count;
                EventSystem.current.SetSelectedGameObject(buttons[at].gameObject);
            }
            else if(key==KeyCode.Return||key==KeyCode.Space)
            {
                // A background control selected before a popup cannot receive its submit.
                if(at<0){EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);return;}
                ExecuteEvents.Execute(buttons[at].gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            }
        }

        void SelectControl(string id)
        {
            if(EventSystem.current==null)return;
            if(id!=null&&_buttons.TryGetValue(id,out Button button)&&button!=null&&button.isActiveAndEnabled&&button.IsInteractable())
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            else EventSystem.current.SetSelectedGameObject(null);
        }

        void SelectPageControl()
        {
            string id=_page==Page.Tutorial?"start-tutorial":_page==Page.Offers?"offer-0":
                _page==Page.Squad?"start-fight":_page==Page.Result?"after-result":
                _page==Page.Fight?"order-0":null;
            if(_page==Page.ChooseTribe&&_campaign.Choosable().Count>0)id="tribe-"+(_gatesReview?KindredSiege.Sim.Learning.CampaignTribes.BloodridgeId:_campaign.Choosable()[0].Id);
            if(_page==Page.Squad&&_squad.Count==0&&_campaign.Reserve.Count>0)id="fighter-"+_campaign.Reserve[0].Id;
            SelectControl(_page==Page.Playback?"pause":id);
        }

        void RestoreSelection(GameObject previous)
        {
            if(EventSystem.current==null)return;
            var button=previous!=null?previous.GetComponent<Button>():null;
            if(button!=null&&button.isActiveAndEnabled&&button.IsInteractable())EventSystem.current.SetSelectedGameObject(previous);
            else SelectPageControl();
        }
    }
}
