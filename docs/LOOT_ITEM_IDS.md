# Loot item master IDs

Reference for `LootAllowlist` / `LootBlocklist` in `[MimesisPlayerEnhancement_LootMultiplicator]`.
Use comma-separated **master IDs** (first column), not localization keys.

Generated from `/home/kalle/.local/share/Steam/steamapps/common/MIMESIS` masterdata on 2026-06-29.
Regenerate after game updates:

```bash
./scripts/generate-loot-item-list.sh
```

Total items: **144** (Consumable, Equipment, Miscellany).

## Quick copy lists

### All master IDs

```
3000,3001,3002,3003,3004,3005,3006,3007,3102,3103,3104,3105,3106,3107,993002,1000,1002,1003,1004,1100,1101,1102,1103,1104,1105,1110,1120,1130,1500,1600,2000,2001,2002,2010,2020,2030,2031,2040,2050,2500,2510,2520,2530,2531,2540,2550,2900,5100,5101,5102,5103,5104,5105,5106,5107,5110,5111,5113,5117,5122,5123,5127,5128,5130,5131,5132,5133,5134,5135,5136,5137,5138,5139,5140,5142,5143,5145,5146,5147,5148,5149,5151,5153,5154,5155,5156,5157,5158,5159,5160,5161,5162,5167,5168,5169,6000,8888,10000,59998,59999,772030,775128,775161,777777,785161,991000,5000,5001,5002,5003,5004,5005,5006,5007,5008,5009,5010,5011,5012,5108,5109,5114,5116,5120,5124,5141,5150,5163,5164,5165,5166,5924,5995,5996,5997,5998,5999,8114,8116,9116,9122,9124,9999,995108
```

## Full table

| Master ID | Type | English name | Internal name key | Loot prefab |
|-----------|------|--------------|-------------------|-------------|
| 3000 | Consumable | Shotgun Shell | `STRING_ITEM_NAME_3000` | `shotgun_shell_box` |
| 3001 | Consumable | Shotgun Shell Box Test | `STRING_ITEM_NAME_3001` | `test_shotgun_shell_box` |
| 3002 | Consumable | Detox Juice (100%) | `STRING_ITEM_NAME_3002` | `consumable_decontaminant_full` |
| 3003 | Consumable | Detox Juice (1%) | `STRING_ITEM_NAME_3003` | `consumable_decontaminant_half` |
| 3004 | Consumable | Detox Juice (23%) | `STRING_ITEM_NAME_3004` | `consumable_decontaminant_half` |
| 3005 | Consumable | Detox Juice (38%) | `STRING_ITEM_NAME_3005` | `consumable_decontaminant_half` |
| 3006 | Consumable | Detox Juice (47%) | `STRING_ITEM_NAME_3006` | `consumable_decontaminant_half` |
| 3007 | Consumable | Detox Juice (71%) | `STRING_ITEM_NAME_3007` | `consumable_decontaminant_half` |
| 3102 | Consumable | HP Juice 100% | `STRING_ITEM_NAME_3102` | `consumable_decontaminant_full_hp` |
| 3103 | Consumable | HP Juice 1% | `STRING_ITEM_NAME_3103` | `consumable_decontaminant_half_hp` |
| 3104 | Consumable | HP Juice 23% | `STRING_ITEM_NAME_3104` | `consumable_decontaminant_half_hp` |
| 3105 | Consumable | HP Juice 38% | `STRING_ITEM_NAME_3105` | `consumable_decontaminant_half_hp` |
| 3106 | Consumable | HP Juice 47% | `STRING_ITEM_NAME_3106` | `consumable_decontaminant_half_hp` |
| 3107 | Consumable | HP Juice 71% | `STRING_ITEM_NAME_3107` | `consumable_decontaminant_half_hp` |
| 993002 | Consumable | Detox Juice (100%) | `STRING_ITEM_NAME_3002` | `consumable_decontaminant_full` |
| 1000 | Equipment | Shotgun | `STRING_ITEM_NAME_1000` | `equipment_shotgun` |
| 1002 | Equipment | Shotgun_forTest | `STRING_ITEM_NAME_1002` | `equipment_shotgun` |
| 1003 | Equipment | Shotgun_forTest | `STRING_ITEM_NAME_1002` | `equipment_shotgun` |
| 1004 | Equipment | Shotgun_forTest | `STRING_ITEM_NAME_1002` | `equipment_shotgun` |
| 1100 | Equipment | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` |
| 1101 | Equipment | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` |
| 1102 | Equipment | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` |
| 1103 | Equipment | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` |
| 1104 | Equipment | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` |
| 1105 | Equipment | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` |
| 1110 | Equipment | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` |
| 1120 | Equipment | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` |
| 1130 | Equipment | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` |
| 1500 | Equipment | Double Barrel Shotgun | `STRING_ITEM_NAME_1500` | `equipment_doublebarrel_shotgun` |
| 1600 | Equipment | Amped Bat | `STRING_ITEM_NAME_1600` | `equipment_ampbat` |
| 2000 | Equipment | Flashlight | `STRING_ITEM_NAME_2000` | `equipment_flashlight` |
| 2001 | Equipment | Compass | `STRING_ITEM_NAME_2010` | `equipment_compass` |
| 2002 | Equipment | Rowdy Chicken | `STRING_ITEM_NAME_2020` | `equipment_detector` |
| 2010 | Equipment | Compass | `STRING_ITEM_NAME_2010` | `equipment_compass` |
| 2020 | Equipment | Rowdy Chicken | `STRING_ITEM_NAME_2020` | `equipment_detector` |
| 2030 | Equipment | Paintball | `STRING_ITEM_NAME_2030` | `equipment_paintball` |
| 2031 | Equipment | Paintball | `STRING_ITEM_NAME_2030` | `equipment_paintball` |
| 2040 | Equipment | Electric Swatter | `STRING_ITEM_NAME_2040` | `equipment_electric_flyswatter` |
| 2050 | Equipment | Toy Puppy | `STRING_ITEM_NAME_2050` | `equipment_barking_puppy` |
| 2500 | Equipment | Mega flashlight | `STRING_ITEM_NAME_2500` | `equipment_upgraded_flashlight` |
| 2510 | Equipment | Treasure compass | `STRING_ITEM_NAME_2510` | `equipment_treasurehuntcompass` |
| 2520 | Equipment | Rowdy Mother Hen | `STRING_ITEM_NAME_2520` | `equipment_detector_egg` |
| 2530 | Equipment | Paint Gun | `STRING_ITEM_NAME_2530` | `equipment_paintgun` |
| 2531 | Equipment | STRING_ITEM_NAME_2531 | `STRING_ITEM_NAME_2531` | `equipment_paintgun` |
| 2540 | Equipment | Stunner | `STRING_ITEM_NAME_2540` | `equipment_electric_field` |
| 2550 | Equipment | MockHound | `STRING_ITEM_NAME_2550` | `equipment_recording_dog` |
| 2900 | Equipment | Crowbar | `STRING_ITEM_NAME_2900` | `equipment_crowbar` |
| 5100 | Equipment | Wire | `STRING_ITEM_NAME_5100` | `miscellnary_wires` |
| 5101 | Equipment | Torn Umbrella | `STRING_ITEM_NAME_5101` | `miscellnary_brokenumbrella` |
| 5102 | Equipment | Keyboard | `STRING_ITEM_NAME_5102` | `miscellnary_keyboard` |
| 5103 | Equipment | Toilet Paper Roll | `STRING_ITEM_NAME_5103` | `miscellnary_toiletpaper` |
| 5104 | Equipment | Rubber Cone | `STRING_ITEM_NAME_5104` | `miscellnary_trafficcone` |
| 5105 | Equipment | Broken Traffic Light | `STRING_ITEM_NAME_5105` | `miscellnary_trafficlight` |
| 5106 | Equipment | Frying Pan | `STRING_ITEM_NAME_5106` | `miscellnary_frypan` |
| 5107 | Equipment | Toilet Seat Cover | `STRING_ITEM_NAME_5107` | `miscellnary_toiletbucket` |
| 5110 | Equipment | Shining Frog | `STRING_ITEM_NAME_5110` | `miscellnary_frog` |
| 5111 | Equipment | Defective Bomb | `STRING_ITEM_NAME_5111` | `miscellnary_timebomb` |
| 5113 | Equipment | Broken Radio | `STRING_ITEM_NAME_5113` | `miscellnary_brokencassette` |
| 5117 | Equipment | Rattle | `STRING_ITEM_NAME_5117` | `miscellnary_rattle` |
| 5122 | Equipment | Cuckoo Clock | `STRING_ITEM_NAME_5122` | `miscellnary_cuckooclock` |
| 5123 | Equipment | lost wallet | `STRING_ITEM_NAME_5123` | `miscellnary_wallet` |
| 5127 | Equipment | Music Box | `STRING_ITEM_NAME_5127` | `miscellnary_musicbox` |
| 5128 | Equipment | Toy Airplane | `STRING_ITEM_NAME_5128` | `miscellnary_toyairplane` |
| 5130 | Equipment | Fan | `STRING_ITEM_NAME_5130` | `miscellnary_fan` |
| 5131 | Equipment | Megaphone | `STRING_ITEM_NAME_5131` | `miscellnary_loudspeaker` |
| 5132 | Equipment | Big Mouth Billy Bass | `STRING_ITEM_NAME_5132` | `miscellnary_billybass` |
| 5133 | Equipment | Pedometer | `STRING_ITEM_NAME_5133` | `miscellnary_pedometer` |
| 5134 | Equipment | Lucky Doll | `STRING_ITEM_NAME_5134` | `miscellnary_voodoodoll` |
| 5135 | Equipment | Portrait | `STRING_ITEM_NAME_5135` | `miscellnary_portrait` |
| 5136 | Equipment | Wall Clock | `STRING_ITEM_NAME_5136` | `miscellnary_clock` |
| 5137 | Equipment | Poop Bag | `STRING_ITEM_NAME_5137` | `miscellnary_poopbag` |
| 5138 | Equipment | Owl Statue | `STRING_ITEM_NAME_5138` | `miscellnary_owlstatue` |
| 5139 | Equipment | Boombox | `STRING_ITEM_NAME_5139` | `miscellnary_boombox` |
| 5140 | Equipment | Mysterious Figurine | `STRING_ITEM_NAME_5140` | `miscellnary_talkingdoll` |
| 5142 | Equipment | Pinwheel | `STRING_ITEM_NAME_5142` | `miscellnary_pinwheel` |
| 5143 | Equipment | Rubber Ducky | `STRING_ITEM_NAME_5143` | `miscellnary_toyduck` |
| 5145 | Equipment | Mug | `STRING_ITEM_NAME_5145` | `miscellnary_mugcup` |
| 5146 | Equipment | Spork | `STRING_ITEM_NAME_5146` | `miscellnary_spork` |
| 5147 | Equipment | Plunger | `STRING_ITEM_NAME_5147` | `miscellnary_plunger` |
| 5148 | Equipment | Quokka Doll | `STRING_ITEM_NAME_5148` | `miscellnary_quokkadoll` |
| 5149 | Equipment | Lighter | `STRING_ITEM_NAME_5149` | `miscellnary_lighter` |
| 5151 | Equipment | Folding Chair | `STRING_ITEM_NAME_5151` | `miscellnary_foldingchair` |
| 5153 | Equipment | Whiteboard | `STRING_ITEM_NAME_5153` | `miscellnary_whiteboard_text1` |
| 5154 | Equipment | Whiteboard | `STRING_ITEM_NAME_5154` | `miscellnary_whiteboard_text2` |
| 5155 | Equipment | Whiteboard | `STRING_ITEM_NAME_5155` | `miscellnary_whiteboard_text3` |
| 5156 | Equipment | Whiteboard | `STRING_ITEM_NAME_5156` | `miscellnary_whiteboard_text4` |
| 5157 | Equipment | Whiteboard | `STRING_ITEM_NAME_5157` | `miscellnary_whiteboard_text5` |
| 5158 | Equipment | Old Sneakers | `STRING_ITEM_NAME_5158` | `miscellnary_shoe` |
| 5159 | Equipment | Rotten Bonsai | `STRING_ITEM_NAME_5159` | `miscellnary_rottenbonsai` |
| 5160 | Equipment | Light Stick | `STRING_ITEM_NAME_5160` | `miscellnary_lightstick` |
| 5161 | Equipment | Spray Bottle | `STRING_ITEM_NAME_5161` | `miscellnary_sprayer` |
| 5162 | Equipment | Cat Wand | `STRING_ITEM_NAME_5162` | `miscellnary_cattoy` |
| 5167 | Equipment | Spray Bottle | `STRING_ITEM_NAME_5167` | `miscellnary_sprayer_polluted` |
| 5168 | Equipment | Warm Poop | `STRING_ITEM_NAME_5168` | `miscellnary_babyrilla_poop` |
| 5169 | Equipment | Golden Poop | `STRING_ITEM_NAME_5169` | `miscellnary_babyrilla_goldenpoop` |
| 6000 | Equipment | Key | `STRING_ITEM_NAME_6000` | `miscellnary_key` |
| 8888 | Equipment | Unknown Black Matter | `STRING_ITEM_NAME_8888` | `miscellnary_unknown_black_matter` |
| 10000 | Equipment | Golden Rattle | `STRING_ITEM_NAME_10000` | `miscellnary_rattle_dl_test` |
| 59998 | Equipment | Projectile Physics Test | `STRING_ITEM_NAME_59998` | `equipment_shotgun` |
| 59999 | Equipment | Projectile Ray Test | `STRING_ITEM_NAME_59999` | `equipment_shotgun` |
| 772030 | Equipment | 페인트 볼 짭(테스트용) | `페인트 볼 짭(테스트용)` | `equipment_paintball` |
| 775128 | Equipment | Toy Airplane | `STRING_ITEM_NAME_5128` | `miscellnary_toyairplane` |
| 775161 | Equipment | 분무기 오염도 낮추기(테스트용) | `분무기 오염도 낮추기(테스트용)` | `miscellnary_sprayer` |
| 777777 | Equipment | One-Hit Bat for DL Request | `STRING_ITEM_NAME_777777` | `equipment_woodbat_dl_test` |
| 785161 | Equipment | 분무기 오염도 올리기(테스트용) | `분무기 오염도 올리기(테스트용)` | `miscellnary_sprayer` |
| 991000 | Equipment | Shotgun | `STRING_ITEM_NAME_1000` | `equipment_shotgun` |
| 5000 | Miscellany | Empty Glass Bottle | `STRING_ITEM_NAME_5000` | `garb_bottle_01` |
| 5001 | Miscellany | Empty Plastic Bottle | `STRING_ITEM_NAME_5001` | `garb_bottle_02` |
| 5002 | Miscellany | Spare Tire | `STRING_ITEM_NAME_5002` | `garb_Tire` |
| 5003 | Miscellany | Truck Engine | `STRING_ITEM_NAME_5003` | `engine_big_truck` |
| 5004 | Miscellany | Empty Gas Canister | `STRING_ITEM_NAME_5004` | `gas_tank_empty` |
| 5005 | Miscellany | Half-Full Gas Canister | `STRING_ITEM_NAME_5005` | `gas_tank_half` |
| 5006 | Miscellany | Full Gas Canister | `STRING_ITEM_NAME_5006` | `gas_tank_full` |
| 5007 | Miscellany | Gasoline Can | `STRING_ITEM_NAME_5007` | `gasoline_can` |
| 5008 | Miscellany | Wastewater Drum | `STRING_ITEM_NAME_5008` | `wastewater_can` |
| 5009 | Miscellany | Beer Bottle | `STRING_ITEM_NAME_5009` | `beer_bottle` |
| 5010 | Miscellany | Pepsi Zero Lime | `STRING_ITEM_NAME_5010` | `pepsi_zero_lime_bottle` |
| 5011 | Miscellany | Toothed Gear | `STRING_ITEM_NAME_5011` | `machine_component` |
| 5012 | Miscellany | Gol-Den Oxygen Tank (Empty) | `STRING_ITEM_NAME_5012` | `oxygen_bottle_golden` |
| 5108 | Miscellany | Dead Battery | `STRING_ITEM_NAME_5108` | `miscellnary_carbattery` |
| 5109 | Miscellany | Dummy | `STRING_ITEM_NAME_5109` | `miscellnary_dummybust` |
| 5114 | Miscellany | Old Tire | `STRING_ITEM_NAME_5114` | `miscellnary_wastetire` |
| 5116 | Miscellany | Dumbbell | `STRING_ITEM_NAME_5116` | `miscellnary_dumbbell` |
| 5120 | Miscellany | Guitar | `STRING_ITEM_NAME_5120` | `miscellnary_guitar` |
| 5124 | Miscellany | Golden Statue | `STRING_ITEM_NAME_5124` | `miscellnary_goldenstatue` |
| 5141 | Miscellany | Cardboard Car | `STRING_ITEM_NAME_5141` | `miscellnary_boxcar` |
| 5150 | Miscellany | Inflatable T-Rex | `STRING_ITEM_NAME_5150` | `miscellnary_dinosaurairsuit` |
| 5163 | Miscellany | Gold Chain | `STRING_ITEM_NAME_5163` | `miscellnary_blingblingchain` |
| 5164 | Miscellany | Petal | `STRING_ITEM_NAME_5164` | `miscellnary_petalhat` |
| 5165 | Miscellany | Gat | `STRING_ITEM_NAME_5165` | `miscellnary_gat` |
| 5166 | Miscellany | Cardboard Plane | `STRING_ITEM_NAME_5166` | `miscellnary_boxairplane` |
| 5924 | Miscellany | Golden Statue | `STRING_ITEM_NAME_5124` | `miscellnary_goldenstatue` |
| 5995 | Miscellany | Frog Hat | `STRING_ITEM_NAME_5995` | `miscellnary_frogheadgear` |
| 5996 | Miscellany | Chicken Hat | `STRING_ITEM_NAME_5996` | `miscellnary_chickenheadgear` |
| 5997 | Miscellany | Pumpkin Hat | `STRING_ITEM_NAME_5997` | `miscellnary_pumpkinheadgear` |
| 5998 | Miscellany | Stylish cap | `STRING_ITEM_NAME_5998` | `miscellnary_hiphat` |
| 5999 | Miscellany | Sunglasses | `STRING_ITEM_NAME_5999` | `miscellnary_sunglass` |
| 8114 | Miscellany | Old Tire | `STRING_ITEM_NAME_5114` | `miscellnary_guitar` |
| 8116 | Miscellany | Dumbbell | `STRING_ITEM_NAME_5116` | `miscellnary_guitar` |
| 9116 | Miscellany | Dumbbell | `STRING_ITEM_NAME_5116` | `miscellnary_dumbbell` |
| 9122 | Miscellany | Cuckoo Clock | `STRING_ITEM_NAME_5122` | `miscellnary_cuckooclock` |
| 9124 | Miscellany | Golden Statue | `STRING_ITEM_NAME_5124` | `miscellnary_goldenstatue` |
| 9999 | Miscellany | Test Item | `STRING_ITEM_NAME_9999` | `gasoline_can` |
| 995108 | Miscellany | Dead Battery | `STRING_ITEM_NAME_5108` | `miscellnary_carbattery` |

## By type


### Consumable

- `3000` — Shotgun Shell
- `3001` — Shotgun Shell Box Test
- `3002` — Detox Juice (100%)
- `3003` — Detox Juice (1%)
- `3004` — Detox Juice (23%)
- `3005` — Detox Juice (38%)
- `3006` — Detox Juice (47%)
- `3007` — Detox Juice (71%)
- `3102` — HP Juice 100%
- `3103` — HP Juice 1%
- `3104` — HP Juice 23%
- `3105` — HP Juice 38%
- `3106` — HP Juice 47%
- `3107` — HP Juice 71%
- `993002` — Detox Juice (100%)

### Equipment

- `1000` — Shotgun
- `1002` — Shotgun_forTest
- `1003` — Shotgun_forTest
- `1004` — Shotgun_forTest
- `1100` — Teddy Bat
- `1101` — Teddy Bat
- `1102` — Teddy Bat
- `1103` — Teddy Bat
- `1104` — Teddy Bat
- `1105` — Teddy Bat
- `1110` — Teddy Bat
- `1120` — Teddy Bat
- `1130` — Teddy Bat
- `1500` — Double Barrel Shotgun
- `1600` — Amped Bat
- `2000` — Flashlight
- `2001` — Compass
- `2002` — Rowdy Chicken
- `2010` — Compass
- `2020` — Rowdy Chicken
- `2030` — Paintball
- `2031` — Paintball
- `2040` — Electric Swatter
- `2050` — Toy Puppy
- `2500` — Mega flashlight
- `2510` — Treasure compass
- `2520` — Rowdy Mother Hen
- `2530` — Paint Gun
- `2531` — STRING_ITEM_NAME_2531
- `2540` — Stunner
- `2550` — MockHound
- `2900` — Crowbar
- `5100` — Wire
- `5101` — Torn Umbrella
- `5102` — Keyboard
- `5103` — Toilet Paper Roll
- `5104` — Rubber Cone
- `5105` — Broken Traffic Light
- `5106` — Frying Pan
- `5107` — Toilet Seat Cover
- `5110` — Shining Frog
- `5111` — Defective Bomb
- `5113` — Broken Radio
- `5117` — Rattle
- `5122` — Cuckoo Clock
- `5123` — lost wallet
- `5127` — Music Box
- `5128` — Toy Airplane
- `5130` — Fan
- `5131` — Megaphone
- `5132` — Big Mouth Billy Bass
- `5133` — Pedometer
- `5134` — Lucky Doll
- `5135` — Portrait
- `5136` — Wall Clock
- `5137` — Poop Bag
- `5138` — Owl Statue
- `5139` — Boombox
- `5140` — Mysterious Figurine
- `5142` — Pinwheel
- `5143` — Rubber Ducky
- `5145` — Mug
- `5146` — Spork
- `5147` — Plunger
- `5148` — Quokka Doll
- `5149` — Lighter
- `5151` — Folding Chair
- `5153` — Whiteboard
- `5154` — Whiteboard
- `5155` — Whiteboard
- `5156` — Whiteboard
- `5157` — Whiteboard
- `5158` — Old Sneakers
- `5159` — Rotten Bonsai
- `5160` — Light Stick
- `5161` — Spray Bottle
- `5162` — Cat Wand
- `5167` — Spray Bottle
- `5168` — Warm Poop
- `5169` — Golden Poop
- `6000` — Key
- `8888` — Unknown Black Matter
- `10000` — Golden Rattle
- `59998` — Projectile Physics Test
- `59999` — Projectile Ray Test
- `772030` — 페인트 볼 짭(테스트용)
- `775128` — Toy Airplane
- `775161` — 분무기 오염도 낮추기(테스트용)
- `777777` — One-Hit Bat for DL Request
- `785161` — 분무기 오염도 올리기(테스트용)
- `991000` — Shotgun

### Miscellany

- `5000` — Empty Glass Bottle
- `5001` — Empty Plastic Bottle
- `5002` — Spare Tire
- `5003` — Truck Engine
- `5004` — Empty Gas Canister
- `5005` — Half-Full Gas Canister
- `5006` — Full Gas Canister
- `5007` — Gasoline Can
- `5008` — Wastewater Drum
- `5009` — Beer Bottle
- `5010` — Pepsi Zero Lime
- `5011` — Toothed Gear
- `5012` — Gol-Den Oxygen Tank (Empty)
- `5108` — Dead Battery
- `5109` — Dummy
- `5114` — Old Tire
- `5116` — Dumbbell
- `5120` — Guitar
- `5124` — Golden Statue
- `5141` — Cardboard Car
- `5150` — Inflatable T-Rex
- `5163` — Gold Chain
- `5164` — Petal
- `5165` — Gat
- `5166` — Cardboard Plane
- `5924` — Golden Statue
- `5995` — Frog Hat
- `5996` — Chicken Hat
- `5997` — Pumpkin Hat
- `5998` — Stylish cap
- `5999` — Sunglasses
- `8114` — Old Tire
- `8116` — Dumbbell
- `9116` — Dumbbell
- `9122` — Cuckoo Clock
- `9124` — Golden Statue
- `9999` — Test Item
- `995108` — Dead Battery
