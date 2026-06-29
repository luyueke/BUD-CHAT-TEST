namespace Game.Avatar {
    public abstract class BaseAvatarData {
        public virtual CharacterPartData GetPartData(int subType) {
            return null;
        }
    }
}
