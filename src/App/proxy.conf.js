const target = process.env.API_URL || 'http://localhost:5266';

if (!process.env.API_URL) {
  console.warn('[proxy] API_URL env var not set — falling back to ' + target);
}

module.exports = [
  {
    context: ['/api'],
    target,
    changeOrigin: true,
    secure: false,
  },
];
