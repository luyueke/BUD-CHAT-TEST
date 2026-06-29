using NetEngine.src;


namespace NetEngine
{
    public class Global
    {
        public static Room Room { get; set; }
        public static Map Map { get; set; }

        //public static void GetRoomList(GetRoomListPara para, Action<ResponseEvent> callback)
        //{
        //    Room.GetRoomList(para, callback);
        //}

        public static void UnInit()
        {
            Listener.Clear();
            Core.UnInitSdk();
        }
    }

    public abstract class UserInfo
    {
    }
}