/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Components/**/*.{razor,html,cshtml}',
    './Pages/**/*.{razor,html,cshtml}',
    './wwwroot/**/*.html',
    './wwwroot/**/*.js'
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          primary: '#1b6ec2',
          accent: '#0077cc',
          border: '#1861ac',
        }
      }
    },
  },
  plugins: [],
}
