import { isCalendarDay } from '../dates';
import type { EasterEggDefinition } from '../types';

export const towelDayEgg: EasterEggDefinition = {
  id: 'towelDay',
  cssClass: 'egg-towel-day',
  priority: 10,
  isActive: (date) => isCalendarDay(date, 5, 25),
  flavorKey: 'dashboard.easter_egg_towelDay_flavor',
};
