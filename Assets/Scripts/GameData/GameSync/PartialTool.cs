// using System;
//
// namespace Common.Network.MsgPb
// {
//     /** Properties of a Frame. */
//     [Serializable]
//     public partial class Frame
//     {
//         public string RoomId { get; set; }
//         public long Time { get; set; }
//         public bool IsReplay { get; set; }
//
//         public Frame(Frame frame, string id)
//         {
//             RoomId = id;
//             Ext.Seed = frame.Ext.Seed;
//             Id = frame.Id;
//             Items.AddRange(frame.Items);
//             Time = 0;
//             IsReplay = false;
//         }
//     }
//
//     public partial class RecvFrameBst
//     {
//         public RecvFrameBst(Frame frame, string id)
//         {
//             Frame = new Frame(frame, id);
//         }
//     }
// }