export const environment = {
  production: false,
  apiBaseUrl: '/api',
  /** 静态资源（头像等）基址；开发环境直连 API，避免 dev server 未代理 /uploads 时图片 404 */
  assetBaseUrl: 'https://localhost:7088',
  requestSignEnabled: false,
  requestSignSecretKey: 'EnterprisePlatform-Request-Sign-Key-Change-Me!',
  tokenStorageKey: 'ep_access_token',
  userStorageKey: 'ep_current_user'
};
