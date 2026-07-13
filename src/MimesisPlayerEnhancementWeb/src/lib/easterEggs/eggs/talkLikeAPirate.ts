import { isCalendarDay } from '../dates';
import type { EasterEggDefinition } from '../types';

export const talkLikeAPirateEgg: EasterEggDefinition = {
  id: 'talkLikeAPirate',
  cssClass: 'egg-talk-like-a-pirate',
  priority: 10,
  isActive: (date) => isCalendarDay(date, 9, 19),
  flavorKey: 'dashboard.easter_egg_talkLikeAPirate_flavor',
};
