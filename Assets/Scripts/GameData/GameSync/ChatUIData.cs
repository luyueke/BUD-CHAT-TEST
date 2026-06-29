namespace GameData.GameSync
{
    public enum ChatType
    {
        Chat,
        Broadcast,
        Emote,
    }

    public class ChatUIData
    {
        public string Text
        {
            get { return _Text; }
			set
			{
				if (_Text == value)
					return;

				_Text = value;
				HasPendingVisualSizeChange = true;
			}
        }

        public string PlayerName { get; set; }

        public ChatType ChatType;

        public bool HasPendingVisualSizeChange { get; set; }

        string _Text;
    }
}