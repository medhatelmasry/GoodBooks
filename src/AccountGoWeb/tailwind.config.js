/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Views/**/*.cshtml', // Razor views in the Views folder
    './wwwroot/**/*.js',   // Any JavaScript files in the wwwroot folder
    './Pages/**/*.cshtml', // If you have Razor Pages in your project
  ],
  theme: {
    extend: {
      // Customize your theme here
      colors: {
        primary: '#1E40AF', // Example: custom primary color
        secondary: '#64748B',
      },
      fontFamily: {
        sans: ['Inter', 'sans-serif'], // Example: custom font family
      },
    },
  },
  plugins: [
    // Add any Tailwind plugins here if needed
    require('@tailwindcss/forms'), // Example: better styling for forms
    require('@tailwindcss/typography'), // Typography plugin
  ],
};
