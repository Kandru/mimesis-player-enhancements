import { aprilFoolsEgg } from './eggs/aprilFools';
import { christmasEgg } from './eggs/christmas';
import { halloweenEgg } from './eggs/halloween';
import { mayTheFourthEgg } from './eggs/mayTheFourth';
import { newYearEgg } from './eggs/newYear';
import { piDayEgg } from './eggs/piDay';
import { programmersDayEgg } from './eggs/programmersDay';
import { starTrekEgg } from './eggs/starTrek';
import { talkLikeAPirateEgg } from './eggs/talkLikeAPirate';
import { towelDayEgg } from './eggs/towelDay';
import { valentinesEgg } from './eggs/valentines';
import type { EasterEggDefinition, EasterEggId } from './types';

export const easterEggRegistry: EasterEggDefinition[] = [
  newYearEgg,
  christmasEgg,
  valentinesEgg,
  starTrekEgg,
  piDayEgg,
  aprilFoolsEgg,
  mayTheFourthEgg,
  towelDayEgg,
  talkLikeAPirateEgg,
  programmersDayEgg,
  halloweenEgg,
];

const byId = new Map<EasterEggId, EasterEggDefinition>(
  easterEggRegistry.map((egg) => [egg.id, egg]),
);

export function getEasterEggById(id: string): EasterEggDefinition | null {
  return byId.get(id as EasterEggId) ?? null;
}

export function getAllEasterEggIds(): EasterEggId[] {
  return easterEggRegistry.map((egg) => egg.id);
}
