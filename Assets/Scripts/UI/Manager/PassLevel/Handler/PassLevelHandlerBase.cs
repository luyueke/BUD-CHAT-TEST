using System;
using Game.Base;
using GameData;

public abstract class PassLevelHandlerBase
{
    public abstract void HandlePassed();
    public abstract void HandleFailed();

    public virtual void HandleResult(PassLevelResult result)
    {
        if (result == PassLevelResult.Passed)
        {
            HandlePassed();
        }
        else if (result == PassLevelResult.Failed)
        {
            HandleFailed();
        }
    }
}

public class PassLevelHandlerFactory
{
    public static PassLevelHandlerBase CreateHandler()
    {
        switch (GameController.GetEnterGameModel())
        {
            //     case EnterGameMode.CreateEmptyScene:
            //     case EnterGameMode.ContinueEditScene:
            //         return new PassLevelEditModeHandler();
            //
            //     case EnterGameMode.GuestScene:
            //         return new PassLevelGuestHandler();
            //     case EnterGameMode.GameMixerGuide:
            //         return new PassLevelGameMixerHandler();
            //     case EnterGameMode.PublishTest:
            //         return new PassLevelPublishTestHandler();
            //     case EnterGameMode.UpdatePublishTest:
            //         return new PassLevelUpdatePublishTestHandler();
            //     case EnterGameMode.DailyChallenge:
            //         return PlayerManager.Inst.IsTransported()
            //             ? new PassLevelGuestHandler()
            //             : new PassLevelDailyChallengeHandler();
            //     case EnterGameMode.WeekChallenge:
            //         return PlayerManager.Inst.IsTransported()
            //             ? new PassLevelGuestHandler()
            //             : new PassLevelWeekChallengeHandler();
            case EnterGameModel.CreateEmptyScene:
            case EnterGameModel.ContinueEditScene:
                return new PassLevelEditModeHandler();
            case EnterGameModel.GuestScene:
                return new PassLevelGuestHandler();
            case EnterGameModel.PublishTest:
                return new PassLevelPublishTestHandler();
            case EnterGameModel.UpdatePublishTest:
                return new PassLevelUpdatePublishTestHandler();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}