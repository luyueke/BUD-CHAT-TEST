/**
 * 注意这里的0 1是设置面板的选项下标，一般0是开，1是关
 */
public class GlobalSettingData
{
 //General
 public GameView gameView = GameView.FirstPerson; //游戏视角
 public int automaticRunning = 0; //自动快跑
 public FlyingMode flyingMode = FlyingMode.Original; //飞行模式
 public int lockMoveStick = 0; //锁定移动摇杆
 public float cameraPanSensitivity = 0.31f; //镜头灵敏度
 public float viewDistance = 250;
 public int showUserName = 0; //显示用户名
 public int friendRequestNew = -1; //好友申请

 public int AssistMode = 0; //是否开启导航，默认开启

 //Graphics
 public int FPS = 1; //帧率 0=60帧，1=30帧

 public int bloom = -1; //后处理


 public int shadow = -1;

 public int graphics = 0;
 //Sound
 public int footStep = 0; //脚步
 public float bgm = 100; //bgm
 public float soundEffect = 100; //除bgm和语音外的其他音效
 public float microPhone = 100; //麦克风
 public float speaker = 100; //扬声器

}

public enum GameView
{
 FirstPerson,
 ThirdPerson
}

public enum FlyingMode
{
 Original,
 Free
}
public enum GraphicsEnum
{
 None,
 High,
 Medium,
 Low
}
