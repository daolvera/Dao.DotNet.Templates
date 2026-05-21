const apiUrl =
  process.env['services__api__https__0'] ||
  process.env['services__api__http__0'] ||
  'https://localhost:7290';

module.exports = {
  '/api': {
    target: apiUrl,
    secure: false,
    changeOrigin: true,
  },
//#if (IncludeSignalR)
  '/hubs': {
    target: apiUrl,
    secure: false,
    changeOrigin: true,
    ws: true,
  },
//#endif
};
