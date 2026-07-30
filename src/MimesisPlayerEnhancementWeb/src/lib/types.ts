export interface StatusDto {
  isConnected: boolean;
  isHost: boolean;
  saveSlotId: number;
  lobbyName: string;
  modVersion: string;
  lastSeenModVersion?: string;
  listenUrl: string;
  snapshotVersion: number;
  configVersion: number;
  joinAnytimeRoutingCount: number;
  locale: string;
  sessionScene?: string;
  blindModeEnabled?: boolean;
  canViewSaveSettings?: boolean;
}

export interface ChangelogAcknowledgeResult {
  success: boolean;
  message: string;
  modVersion: string;
  lastSeenModVersion: string;
}

export interface EntityCountEntryDto {
  key: string;
  displayName: string;
  localizationKey?: string;
  count: number;
}

export interface StatCountersDto {
  score?: number;
  trainValueDeposited?: number;
  itemsDeposited?: number;
  itemsCarried?: number;
  monsterKillsTotal?: number;
  friendsKilled?: number;
  killedByFriends?: number;
  deaths?: number;
  trapDeathsTotal?: number;
  revives?: number;
  dungeonExitsAlive?: number;
  dungeonExitsDead?: number;
  survivalWins?: number;
  survivalLeftBehind?: number;
  deathmatchWins?: number;
  deathmatchDeaths?: number;
  damageToFriend?: number;
  mimicEncounters?: number;
  connectedSeconds?: number;
  medianLifetimeMs?: number | null;
  monsterKillBreakdown?: EntityCountEntryDto[];
  deathsByMonsterBreakdown?: EntityCountEntryDto[];
  deathsByTrapBreakdown?: EntityCountEntryDto[];
}

export interface SessionStatsDto extends StatCountersDto {}

export type DungeonRunOutcome = 'success' | 'failed' | 'abandoned' | 'in_progress';

export interface StatisticsHistoryPlayerRowDto {
  steamId: string;
  displayName: string;
  counters: StatCountersDto;
}

export interface StatisticsHistoryRunDto {
  runId: string;
  zone: number;
  cycle: number;
  seed: number;
  mapId: number;
  mapKey: string;
  mapName: string;
  dungeonMasterId?: number;
  startedAtUtc: string;
  endedAtUtc?: string;
  durationSeconds?: number | null;
  outcome: DungeonRunOutcome;
  totals: StatCountersDto;
  players: StatisticsHistoryPlayerRowDto[];
}

export interface StatisticsHistoryZoneDto {
  zone: number;
  isCurrent: boolean;
  startedAtUtc?: string;
  endedAtUtc?: string;
  trimmedRunCount: number;
  totals: StatCountersDto;
  players: StatisticsHistoryPlayerRowDto[];
  runs: StatisticsHistoryRunDto[];
}

export interface StatisticsHistoryDto {
  saveSlotId: number;
  currentZone: number;
  historyRevision: number;
  updatedAtUtc: string;
  trimmedZoneCount: number;
  zones: StatisticsHistoryZoneDto[];
}

export interface LeaderboardEntryDto {
  steamId: string;
  displayName: string;
  score: number;
  allTimeScore: number;
  highestZoneReached: number;
  runRestarts: number;
  sessionsCompleted: number;
  dungeonRunsPlayed: number;
  global: StatCountersDto;
  currentZone: StatCountersDto;
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
  totalStats?: SessionStatsDto;
  runStats?: SessionStatsDto;
  activityState?: string;
  activityDetail?: string;
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
  currentZone: number;
  historyRevision: number;
  updatedAtUtc: string;
  connectedSteamIds: string[];
  serverGlobalTotals: StatCountersDto;
  serverZoneTotals: StatCountersDto;
  entries: LeaderboardEntryDto[];
}

export interface PlayerZoneStatsDto {
  zone: number;
  counters: StatCountersDto;
}

export interface PlayerRunStatsDto {
  runId: string;
  zone: number;
  cycle: number;
  seed: number;
  mapKey: string;
  mapName: string;
  outcome: DungeonRunOutcome;
  endedAtUtc?: string;
  counters: StatCountersDto;
}

export interface PlayerStatsDto {
  steamId: string;
  displayName: string;
  global: {
    counters: StatCountersDto;
    highestZoneReached: number;
    runRestarts: number;
    sessionsCompleted: number;
    dungeonRunsPlayed: number;
    voiceEvents?: number;
  };
  currentZone?: PlayerZoneStatsDto;
  zones: PlayerZoneStatsDto[];
  recentRuns: PlayerRunStatsDto[];
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
  floorSpan?: number[];
  floorState?: 'active' | 'inactive' | 'connector';
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
  label?: string;
}

export interface MinimapAreaDto {
  id: string;
  label: string;
  kind: string;
  borderless?: boolean;
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
  spanX?: number;
  spanZ?: number;
}

export interface MinimapPoiDto {
  kind: string;
  x: number;
  z: number;
  label?: string;
  areaId?: string;
}

export interface MinimapPayload {
  layoutVersion: number;
  layoutKind: string;
  displayMode: string;
  sceneLabel: string;
  defaultAreaId: string;
  bounds: MinimapBoundsDto;
  areas?: MinimapAreaDto[];
  tiles: MinimapTileDto[];
  connections?: Array<{ from: string; to: string }>;
  markers: MinimapMarkerDto[];
  train?: MinimapTrainDto | null;
  pointsOfInterest?: MinimapPoiDto[];
  activeAreaId?: string;
  activeAreaLabel?: string;
  activeFloorIndex?: number;
  connectionPoints?: MinimapConnectionPointDto[];
  blindMode?: boolean;
  compositeIndoor?: boolean;
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
  description?: string;
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

export interface MonsterOptionDto {
  id: string;
  label: string;
  type: string;
  masterId?: number;
}

export interface UiDebugStatusDto {
  ingame: boolean;
  alive: boolean;
  maxPlayers: number;
  spectator: boolean;
  loadingWait: boolean;
  escMenu: boolean;
  survivalResult: boolean;
}

export interface UiDebugToggleResultDto {
  success: boolean;
  id: string;
  active: boolean;
  message: string;
}

export type RouteName =
  | 'home'
  | 'changelog'
  | 'donation'
  | 'global-settings'
  | 'players'
  | 'minimap'
  | 'leaderboard'
  | 'settings'
  | 'player';
