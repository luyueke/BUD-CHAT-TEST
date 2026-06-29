namespace UI.EditOperation
{
    public enum OperationType
    {
        // 编辑面板设置
        Move = 1,
        Rotate,
        Scale,
        Copy,
        Lock,
        Hide,
        PublishProp,

        //全局面板设置
        CanDelete,
        CanCombine,

        //Properties内设置
        Properties,
        Animation,
        Movement,
        Visibility,
        Transaction,
        Interaction,
        Collision,
        Rendering
    }
}