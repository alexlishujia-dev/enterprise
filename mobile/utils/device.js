/** 多端差异化适配 */
export function getPlatform() {
  // #ifdef MP-WEIXIN
  return 'mp-weixin';
  // #endif
  // #ifdef APP-PLUS
  return uni.getSystemInfoSync().platform === 'ios' ? 'ios' : 'android';
  // #endif
  // #ifdef H5
  return 'h5';
  // #endif
  return 'unknown';
}

export function getStatusBarHeight() {
  return uni.getSystemInfoSync().statusBarHeight || 0;
}

export function getSafeAreaBottom() {
  const info = uni.getSystemInfoSync();
  return info.safeAreaInsets?.bottom || 0;
}

export function isMiniProgram() {
  // #ifdef MP-WEIXIN
  return true;
  // #endif
  return false;
}

export function showPlatformToast(title) {
  uni.showToast({ title, icon: 'none' });
}
