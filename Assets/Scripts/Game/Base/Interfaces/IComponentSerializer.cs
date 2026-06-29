/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-18 15:26:54
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-07-18 17:20:39
 * @ Description: Component 序列化接口
 */

using Pb.Map;

namespace Game.Base
{
    public interface IComponentSerializer
    {
        public void Read(PComponentData componentData);
        public PComponentData Write();
    }
}