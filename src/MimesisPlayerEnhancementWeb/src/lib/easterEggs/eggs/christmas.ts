import { isInRange } from '../dates';
import type { EasterEggDefinition } from '../types';

export const christmasEgg: EasterEggDefinition = {
  id: 'christmas',
  cssClass: 'egg-christmas',
  priority: 2,
  isActive: (date) => isInRange(date, 12, 24, 12, 26),
  flavorKey: 'dashboard.easter_egg_christmas_flavor',
  overlay: 'snow',
};
