export interface StatusDto {
  isConnected: boolean;
  isHost: boolean;
  saveSlotId: number;
  lobbyName: string;
  modVersion: string;
  listenUrl: string;
  snapshotVersion: number;
  configVersion: number;
  joinAnytimeRoutingCount: number;
  locale: string;
}

export interface SessionStatsDto {
  currencyEarned?: number;
  survivalDeaths?: number;
  survivalWins?: number;
  survivalLeftBehind?: number;
  deathmatchDeaths?: number;
  deathmatchWins?: number;
  revives?: number;
  mimicEncounterCount?: number;
  itemCarryCount?: number;
  damageToFriend?: number;
  friendsKilled?: number;
  totalConnectedSeconds?: number;
  monsterKills?: Record<string, number>;
  deathsByMonster?: Record<string, number>;
  deathsByTrap?: Record<string, number>;
}

export interface PlayerDto {
  steamId: string | number;
  playerUid: number;
  displayName: string;
  isHost: boolean;
  isLocal: boolean;
  isBanned: boolean;
  isAlive: boolean;
  networkGrade: number;
  connectionRole: string;
  connectionAddress: string;
  voiceLineCount: number;
  currentSession?: SessionStatsDto;
  health?: number;
  maxHealth?: number;
  toxicPercent?: number;
  lateJoinPhase?: string;
  lateJoinLabel?: string;
  lateJoinStuckSeconds?: number;
  lateJoinAttemptCount?: number;
  godMode?: boolean;
  noClip?: boolean;
}

export interface LeaderboardDto {
  saveSlotId: number;
  connectedSteamIds: string[];
  entries: Array<{ steamId: string; displayName: string; [key: string]: unknown }>;
}

export interface MinimapBoundsDto {
  minX: number;
  minZ: number;
  maxX: number;
  maxZ: number;
}

export interface MinimapTileDto {
  id: string;
  label: string;
  x: number;
  z: number;
  w: number;
  h: number;
  isMainPath: boolean;
  floorIndex?: number;
  centerY?: number;
  multiFloor?: boolean;
}

export interface MinimapConnectionPointDto {
  x: number;
  z: number;
  dirX: number;
  dirZ: number;
  fromTileId: string;
  toTileId: string;
  targetAreaId: string;
  crossArea: boolean;
  crossFloor?: boolean;
  width?: number;
  destX?: number;
  destZ?: number;
  destAreaId?: string;
  teleporterId?: string;
}

export interface MinimapAreaDto {
  id: string;
  label: string;
  kind: string;
  bounds: MinimapBoundsDto;
  tiles: MinimapTileDto[];
  connectionPoints: MinimapConnectionPointDto[];
}

export interface MinimapMarkerDto {
  steamId: string | number;
  displayName: string;
  x: number;
  z: number;
  yaw: number;
  roomName: string;
  areaId: string;
  tileId: string;
  isAlive: boolean;
  isHost: boolean;
  isLocal: boolean;
  floorIndex?: number;
}

export interface MinimapTrainDto {
  x: number;
  z: number;
  yaw: number;
  areaId: string;
}

export interface MinimapLayoutDto {
  layoutVersion: number;
  layoutKind: string;
  displayMode: string;
  sceneLabel: string;
  defaultAreaId: string;
  bounds: MinimapBoundsDto;
  areas: MinimapAreaDto[];
  tiles: MinimapTileDto[];
  connections: Array<{ from: string; to: string }>;
}

export interface MinimapPayload {
  layout: MinimapLayoutDto;
  markers: MinimapMarkerDto[];
  train?: MinimapTrainDto | null;
  activeAreaId?: string;
  blindMode?: boolean;
}

export interface SnapshotPayload {
  status: StatusDto;
  players: PlayerDto[];
  leaderboard?: LeaderboardDto | null;
  minimap?: MinimapPayload | null;
  playersLiveOnly?: boolean;
}

export interface ConfigSelectOption {
  value: string;
  label: string;
}

export interface ConfigEntryDto {
  key: string;
  title: string;
  description: string;
  type: string;
  value: string;
  defaultValue: string;
  globalValue: string;
  isOverridden: boolean;
  isHidden: boolean;
  hasLocalEffect: boolean;
  minValue?: string;
  maxValue?: string;
  inputKind: string;
  entryGroup: string;
  dependsOnKey?: string;
  dependsOnValue?: string;
  selectOptions: ConfigSelectOption[];
}

export interface ConfigSectionDto {
  id: string;
  title: string;
  featureToggle?: ConfigEntryDto;
  entries: ConfigEntryDto[];
}

export interface SettingsDto {
  configPath: string;
  configVersion: number;
  saveSlotId?: number;
  scope: string;
  sections: ConfigSectionDto[];
  profile?: { mode: string; presetId: string; label: string };
}

export interface QuickPresetDto {
  id: string;
  name: string;
  description?: string;
  isBuiltin: boolean;
  revision: number;
  mode?: string;
}

export interface ItemOptionDto {
  id: string;
  label: string;
  type: string;
  masterId?: number;
  sellPriceMin?: number;
  variants?: Array<{ percent: number; masterId: number }>;
}

export interface PlayerStatsDto {
  steamId: string;
  displayName: string;
  global: { counters: Record<string, number>; sessionsCompleted: number };
  currentSession?: { counters: Record<string, number> };
  recentSessions?: Array<Record<string, unknown>>;
}

export type RouteName =
  | 'home'
  | 'donation'
  | 'global-settings'
  | 'players'
  | 'minimap'
  | 'leaderboard'
  | 'settings'
  | 'player';
