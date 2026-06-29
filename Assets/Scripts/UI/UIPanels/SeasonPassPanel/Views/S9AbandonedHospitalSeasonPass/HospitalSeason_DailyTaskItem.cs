using Game.Event;
using UnityEngine.UI;

public class HospitalSeason_DailyTaskItem : DailyTaskItem
{
    public Image Img_Progress;

    public override void SetData(TaskItemData itemData)
    {
        base.SetData(itemData);
        switch ((ClaimStatus)itemData.eventStatus) {
            case ClaimStatus.Claimed:
                Img_Progress.fillAmount = 1;
                break;
            
            case ClaimStatus.Unlocked:
            case ClaimStatus.Lock:
                Img_Progress.fillAmount = 0.5f;
                break;
        }
    }
}
