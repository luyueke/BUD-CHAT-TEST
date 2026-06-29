using System.Collections.Generic;

    public class PlayerStateData
    {
        //public EmoteData emoteData;

        public LinkedList<PlayerState> currentStateList = new LinkedList<PlayerState>();
        public LinkedList<PlayerState> cacheStateList = new LinkedList<PlayerState>();
        public List<ExcludeStateAction> excludeStateActionList = new List<ExcludeStateAction>();

        public List<StateEvent> stateEventList = new List<StateEvent>();

        public PlayerStateData()
        {
            currentStateList.AddFirst(PlayerState.Default);
        }
    }

    public class ExcludeStateAction : ExcludeStateID
    {
        public StateExcludeType actionType;
    }

    public class ExcludeStateID
    {
        public PlayerState stateID;
        public PlayerState beState;
    }


    //public class EmoteData
    //{
    //    public int emoteID;
    //    public int randomID;
    //}