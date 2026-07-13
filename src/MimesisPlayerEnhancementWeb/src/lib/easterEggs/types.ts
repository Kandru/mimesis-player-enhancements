export type EasterEggId =
  | 'newYear'
  | 'christmas'
  | 'valentines'
  | 'starTrek'
  | 'piDay'
  | 'aprilFools'
  | 'mayTheFourth'
  | 'towelDay'
  | 'talkLikeAPirate'
  | 'programmersDay'
  | 'halloween';

export type EasterEggOverlay = 'snow' | 'stars' | 'hearts' | 'confetti' | 'dots';

export interface EasterEggDefinition {
  id: EasterEggId;
  cssClass: string;
  priority: number;
  isActive(date: Date): boolean;
  flavorKey: string;
  overlay?: EasterEggOverlay;
}
