mergeInto(LibraryManager.library, {
  SetOpenId: function (openid) {
    var openidStr = UTF8ToString(openid);
    GameGlobal.dnSDK.setOpenId(openidStr);
  },
  onPurchase: function (purchaseValue) {
    GameGlobal.dnSDK.onPurchase(purchaseValue);
  },
  onRegister: function () {
    GameGlobal.dnSDK.onRegister();
  },
  onReActive: function (backFlowDay) {
    GameGlobal.dnSDK.track('RE_ACTIVE', {
      backFlowDay: backFlowDay
    });
  },
  onAddToWishlist: function (type) {
    var typeStr = UTF8ToString(type);
    GameGlobal.dnSDK.track('ADD_TO_WISHLIST', {
      type: typeStr
    });
  },
  onShare: function (target) {
    var targetStr = UTF8ToString(target);
    GameGlobal.dnSDK.track('SHARE', {
      target: targetStr
    });
  },
  onCreateRole: function (roleName) {
    var roleNameStr = UTF8ToString(roleName);
    GameGlobal.dnSDK.onCreateRole(roleNameStr);
  },
  onTutorialFinish: function () {
    GameGlobal.dnSDK.onTutorialFinish();
  },
  onUpdateLevel: function (level, power) {
    GameGlobal.dnSDK.track('UPDATE_LEVEL', {
      level: level,
      power: power,
    });
  },
  onViewContent: function (item) {
    var itemStr = UTF8ToString(item);
    GameGlobal.dnSDK.track('VIEW_CONTENT', {
      item: itemStr,
    });
  },
});
