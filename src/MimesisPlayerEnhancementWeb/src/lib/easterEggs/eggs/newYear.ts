import { isAnyCalendarDay } from '../dates';
import type { EasterEggDefinition } from '../types';

export const newYearEgg: EasterEggDefinition = {
  id: 'newYear',
  cssClass: 'egg-new-year',
  priority: 1,
  isActive: (date) => isAnyCalendarDay(date, [[12, 31], [1, 1]]),
  flavorKey: 'dashboard.easter_egg_newYear_flavor',
  overlay: 'confetti',
};
