import { isCalendarDay } from '../dates';
import type { EasterEggDefinition } from '../types';

export const aprilFoolsEgg: EasterEggDefinition = {
  id: 'aprilFools',
  cssClass: 'egg-april-fools',
  priority: 10,
  isActive: (date) => isCalendarDay(date, 4, 1),
  flavorKey: 'dashboard.easter_egg_aprilFools_flavor',
};
