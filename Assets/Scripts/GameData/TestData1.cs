using GameData.BindPropertyDefine;

namespace GameData
{
    /// <summary>
    /// 测试数据模板，外部需要监听数据变化时可参考
    /// </summary>
    public class TestData1 : DataObject<TestData1>
    {
        public BindString tip = new BindString("asdadad");
        public BindInt number = new BindInt(666);
        public BindVector3 pos = new BindVector3(1, 1, 1);

        public override void SetData(TestData1 newData)
        {
            // 判断class对象是否修改
            var isChanged = this.tip.Value != newData.tip.Value ||
                            this.number.Value != newData.number.Value ||
                            !this.pos.Value.Equals(newData.pos.Value);

            var oldV = this.Copy();

            //数据赋值
            this.tip.Value = newData.tip.Value;
            this.number.Value = newData.number.Value;
            this.pos.Value = newData.pos.Value;

            // 抛出数据变更事件
            if (isChanged) OnValueChanged.Invoke(oldV, this);
        }

        public override TestData1 Copy()
        {
            // 深拷贝各bind对象
            return new TestData1()
            {
                tip = this.tip.CopySelf(),
                number = this.number.CopySelf(),
                pos = this.pos.CopySelf(),
            };
        }
    }
}