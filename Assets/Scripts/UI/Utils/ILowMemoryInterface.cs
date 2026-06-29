//仅需要释放图片资源继承（IOS低端机）
public interface ILowMemoryInterface
{
    void ChangeRawImageState(bool isRelease);
}