import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

/**
 * Primary palette generated from #009ccc (H:194°, S:100%, L:40%)
 * Secondary color #0052cc is exposed as CSS variables (--cc-secondary-*).
 */
const CardCheesiTheme = definePreset(Aura, {
  semantic: {
    primary: {
      50:  '#ebf9fd',
      100: '#c4eef9',
      200: '#91def5',
      300: '#4dc8ef',
      400: '#0ab6e6',
      500: '#009ccc',
      600: '#007fa8',
      700: '#006285',
      800: '#004660',
      900: '#002d3d',
      950: '#001e29',
    },
    colorScheme: {
      light: {
        primary: {
          color:         '{primary.500}',
          contrastColor: '#ffffff',
          hoverColor:    '{primary.600}',
          activeColor:   '{primary.700}',
        },
      },
      dark: {
        primary: {
          color:         '{primary.400}',
          contrastColor: '#ffffff',
          hoverColor:    '{primary.300}',
          activeColor:   '{primary.200}',
        },
      },
    },
  },
});

export default CardCheesiTheme;
