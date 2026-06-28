/**
 * 环境配置 — 与 Angular frontend/src/environments 对齐
 *
 * App / 小程序调试：将 apiBaseUrl、assetBaseUrl 改为局域网 IP，例如：
 *   apiBaseUrl: 'http://192.168.1.100:5089/api'
 *   assetBaseUrl: 'http://192.168.1.100:5089'
 */
export default {
  production: false,
  /** H5 开发走 vite 代理；真机/App 填完整 API 地址 */
  apiBaseUrl: '/api',
  assetBaseUrl: 'https://localhost:7088',
  requestSignEnabled: false,
  requestSignSecretKey: 'EnterprisePlatform-Request-Sign-Key-Change-Me!',
  tokenStorageKey: 'ep_access_token',
  userStorageKey: 'ep_current_user',
  defaultPageSize: 20,
  requestTimeout: 30000
};
