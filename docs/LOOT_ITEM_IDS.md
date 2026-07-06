# Loot item master IDs

Reference for `LootAllowlist` / `LootBlocklist` in `[MimesisPlayerEnhancement_LootMultiplicator]`.
Use comma-separated **master IDs** (first column), not localization keys.

Generated from `/home/kalle/.local/share/Steam/steamapps/common/MIMESIS` masterdata on 2026-07-06.
Regenerate after game updates:

```bash
./scripts/generate-loot-item-list.sh
```

Total items: **144** — Consumable **15**, Equipment **91**, Miscellany **38**.

Each section lists every item from game masterdata (`ItemConsumable.json`, `ItemEquipment.json`,
`ItemMiscellany.json`). **Key properties** lists gameplay-relevant fields that differ from the
type default — effect strength, ammo/stack counts, durability, sell price, upgrade path, etc.
(`—` = no distinguishing values among those fields.)

## Quick copy — all master IDs

```
3000,3001,3002,3003,3004,3005,3006,3007,3102,3103,3104,3105,3106,3107,993002,1000,1002,1003,1004,1100,1101,1102,1103,1104,1105,1110,1120,1130,1500,1600,2000,2001,2002,2010,2020,2030,2031,2040,2050,2500,2510,2520,2530,2531,2540,2550,2900,5100,5101,5102,5103,5104,5105,5106,5107,5110,5111,5113,5117,5122,5123,5127,5128,5130,5131,5132,5133,5134,5135,5136,5137,5138,5139,5140,5142,5143,5145,5146,5147,5148,5149,5151,5153,5154,5155,5156,5157,5158,5159,5160,5161,5162,5167,5168,5169,6000,8888,10000,59998,59999,772030,775128,775161,777777,785161,991000,5000,5001,5002,5003,5004,5005,5006,5007,5008,5009,5010,5011,5012,5108,5109,5114,5116,5120,5124,5141,5150,5163,5164,5165,5166,5924,5995,5996,5997,5998,5999,8114,8116,9116,9122,9124,9999,995108
```

## Consumable (15)

| Master ID | English name | Name key | Loot prefab | Key properties |
|-----------|--------------|----------|-------------|----------------|
| 3000 | Shotgun Shell | `STRING_ITEM_NAME_3000` | `shotgun_shell_box` | `bullet_type`=1; `consume_type`=1; `weight`=100 |
| 3001 | Shotgun Shell Box Test | `STRING_ITEM_NAME_3001` | `test_shotgun_shell_box` | `bullet_type`=1; `consume_type`=1; `default_provide_count`=999; `max_stack_count`=999; `price_for_sell_max`=1; `price_for_sell_min`=1; `weight`=0 |
| 3002 | Detox Juice (100%) | `STRING_ITEM_NAME_3002` | `consumable_decontaminant_full` | `actions`=[CHANGE_MUTABLE_STAT/CONTA/-10000]; `price_for_sell_max`=45; `price_for_sell_min`=45 |
| 3003 | Detox Juice (1%) | `STRING_ITEM_NAME_3003` | `consumable_decontaminant_half` | `actions`=[CHANGE_MUTABLE_STAT/CONTA/-100]; `weight`=1500 |
| 3004 | Detox Juice (23%) | `STRING_ITEM_NAME_3004` | `consumable_decontaminant_half` | `actions`=[CHANGE_MUTABLE_STAT/CONTA/-2300]; `weight`=1750 |
| 3005 | Detox Juice (38%) | `STRING_ITEM_NAME_3005` | `consumable_decontaminant_half` | `actions`=[CHANGE_MUTABLE_STAT/CONTA/-3800]; `weight`=2000 |
| 3006 | Detox Juice (47%) | `STRING_ITEM_NAME_3006` | `consumable_decontaminant_half` | `actions`=[CHANGE_MUTABLE_STAT/CONTA/-4700]; `weight`=2250 |
| 3007 | Detox Juice (71%) | `STRING_ITEM_NAME_3007` | `consumable_decontaminant_half` | `actions`=[CHANGE_MUTABLE_STAT/CONTA/-7100]; `weight`=2500 |
| 3102 | HP Juice 100% | `STRING_ITEM_NAME_3102` | `consumable_decontaminant_full_hp` | `actions`=[CHANGE_MUTABLE_STAT/HP/-100]; `price_for_sell_max`=45; `price_for_sell_min`=45 |
| 3103 | HP Juice 1% | `STRING_ITEM_NAME_3103` | `consumable_decontaminant_half_hp` | `actions`=[CHANGE_MUTABLE_STAT/HP/-1]; `weight`=1500 |
| 3104 | HP Juice 23% | `STRING_ITEM_NAME_3104` | `consumable_decontaminant_half_hp` | `actions`=[CHANGE_MUTABLE_STAT/HP/-23]; `weight`=1750 |
| 3105 | HP Juice 38% | `STRING_ITEM_NAME_3105` | `consumable_decontaminant_half_hp` | `actions`=[CHANGE_MUTABLE_STAT/HP/-38]; `weight`=2000 |
| 3106 | HP Juice 47% | `STRING_ITEM_NAME_3106` | `consumable_decontaminant_half_hp` | `actions`=[CHANGE_MUTABLE_STAT/HP/-47]; `weight`=2250 |
| 3107 | HP Juice 71% | `STRING_ITEM_NAME_3107` | `consumable_decontaminant_half_hp` | `actions`=[CHANGE_MUTABLE_STAT/HP/-71]; `weight`=2500 |
| 993002 | Detox Juice (100%) | `STRING_ITEM_NAME_3002` | `consumable_decontaminant_full` | `actions`=[CHANGE_MUTABLE_STAT/CONTA/-10000]; `is_promotion_item`=true; `price_for_sell_max`=45; `price_for_sell_min`=45 |

## Equipment (91)

| Master ID | English name | Name key | Loot prefab | Key properties |
|-----------|--------------|----------|-------------|----------------|
| 1000 | Shotgun | `STRING_ITEM_NAME_1000` | `equipment_shotgun` | `dec_gauge_per_use`=1; `hand_weapon_type`=1; `item_upgrade_cost`=150; `item_upgradedid`=1500; `max_durability`=22; `max_gauge`=1; `min_durability`=18; `skill_list`=[10900]; `skill_reload`=10901; `use_item_upgrade`=true; `visible_gauge_count`=true; `weight`=12000 |
| 1002 | Shotgun_forTest | `STRING_ITEM_NAME_1002` | `equipment_shotgun` | `dec_gauge_per_use`=1; `default_provide_gauge`=9999; `hand_weapon_type`=1; `max_gauge`=9999; `skill_list`=[10900]; `skill_reload`=10901; `visible_gauge_count`=true; `weight`=4000 |
| 1003 | Shotgun_forTest | `STRING_ITEM_NAME_1002` | `equipment_shotgun` | `dec_gauge_per_use`=1; `default_provide_gauge`=9999; `hand_weapon_type`=1; `max_gauge`=9999; `skill_list`=[10900]; `skill_reload`=10901; `visible_gauge_count`=true; `weight`=4000 |
| 1004 | Shotgun_forTest | `STRING_ITEM_NAME_1002` | `equipment_shotgun` | `dec_gauge_per_use`=1; `default_provide_gauge`=9999; `hand_weapon_type`=1; `max_gauge`=9999; `skill_list`=[10900]; `skill_reload`=10901; `visible_gauge_count`=true; `weight`=4000 |
| 1100 | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` | `is_two_hand`=true; `item_upgrade_cost`=150; `item_upgradedid`=1600; `max_durability`=45; `min_durability`=35; `skill_list`=[10800]; `use_item_upgrade`=true; `weight`=5000 |
| 1101 | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` | `is_two_hand`=true; `skill_list`=[10800]; `weight`=200 |
| 1102 | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` | `is_two_hand`=true; `skill_list`=[10800]; `weight`=200 |
| 1103 | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` | `is_two_hand`=true; `skill_list`=[10800]; `weight`=200 |
| 1104 | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` | `is_two_hand`=true; `max_durability`=30; `min_durability`=20; `skill_list`=[10804] |
| 1105 | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` | `is_two_hand`=true; `max_durability`=30; `min_durability`=20; `skill_list`=[10805] |
| 1110 | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` | `is_two_hand`=true; `max_durability`=30; `min_durability`=20; `skill_list`=[10810] |
| 1120 | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` | `is_two_hand`=true; `max_durability`=30; `min_durability`=20; `skill_list`=[10820] |
| 1130 | Teddy Bat | `STRING_ITEM_NAME_1100` | `equipment_woodbat` | `is_two_hand`=true; `max_durability`=30; `min_durability`=20; `skill_list`=[10830] |
| 1500 | Double Barrel Shotgun | `STRING_ITEM_NAME_1500` | `equipment_doublebarrel_shotgun` | `dec_gauge_per_use`=1; `hand_weapon_type`=1; `max_durability`=44; `max_gauge`=2; `min_durability`=36; `skill_list`=[10900]; `skill_reload`=10902; `visible_gauge_count`=true; `weight`=14000 |
| 1600 | Amped Bat | `STRING_ITEM_NAME_1600` | `equipment_ampbat` | `is_two_hand`=true; `max_durability`=88; `min_durability`=72; `skill_gauge_on`=[10871, 10870]; `skill_list`=[10871, 10870]; `weight`=10000 |
| 2000 | Flashlight | `STRING_ITEM_NAME_2000` | `equipment_flashlight` | `charge_affix`=[, %]; `dec_gauge_initial_only`=1; `dec_gauge_per_use`=2; `dec_gauge_use_period`=4800; `default_provide_gauge`=100; `equip_type`=2; `hand_weapon_type`=0; `item_upgrade_cost`=150; `item_upgradedid`=2500; `max_gauge`=100; `stat_list`=[]; `use_charge`=true; `use_item_upgrade`=true; `visible_gauge_count`=true; `weight`=3000 |
| 2001 | Compass | `STRING_ITEM_NAME_2010` | `equipment_compass` | `charge_affix`=[, %]; `dec_gauge_initial_only`=5; `dec_gauge_per_use`=2; `dec_gauge_use_period`=6000; `default_provide_gauge`=100; `equip_type`=2; `hand_weapon_type`=0; `handheld_auraskill_by_gauge`=true; `handheld_auraskill_id`=90002; `max_gauge`=100; `stat_list`=[]; `use_charge`=true; `visible_gauge_count`=true; `weight`=1000 |
| 2002 | Rowdy Chicken | `STRING_ITEM_NAME_2020` | `equipment_detector` | `equip_type`=4; `hand_weapon_type`=0; `handheld_auraskill_id`=90002; `stat_list`=[]; `weight`=500 |
| 2010 | Compass | `STRING_ITEM_NAME_2010` | `equipment_compass` | `equip_type`=3; `hand_weapon_type`=0; `item_upgrade_cost`=150; `item_upgradedid`=2510; `stat_list`=[]; `use_item_upgrade`=true; `weight`=3000 |
| 2020 | Rowdy Chicken | `STRING_ITEM_NAME_2020` | `equipment_detector` | `equip_type`=4; `hand_weapon_type`=0; `item_upgrade_cost`=10; `item_upgradedid`=1000; `stat_list`=[]; `weight`=3000 |
| 2030 | Paintball | `STRING_ITEM_NAME_2030` | `equipment_paintball` | `item_upgrade_cost`=150; `item_upgradedid`=2530; `max_durability`=5; `min_durability`=5; `skill_gauge_on`=[10840]; `skill_list`=[10840]; `stat_list`=[]; `use_item_upgrade`=true; `visible_durability_count`=true; `weight`=1000 |
| 2031 | Paintball | `STRING_ITEM_NAME_2030` | `equipment_paintball` | `max_durability`=5; `min_durability`=5; `skill_gauge_on`=[10844]; `skill_list`=[10844]; `stat_list`=[]; `visible_durability_count`=true; `weight`=1000 |
| 2040 | Electric Swatter | `STRING_ITEM_NAME_2040` | `equipment_electric_flyswatter` | `charge_affix`=[, %]; `default_provide_gauge`=100; `item_upgrade_cost`=150; `item_upgradedid`=2540; `max_durability`=55; `max_gauge`=100; `min_durability`=45; `skill_gauge_on`=[10850]; `skill_list`=[10860]; `use_charge`=true; `use_item_upgrade`=true; `visible_gauge_count`=true; `weight`=7000 |
| 2050 | Toy Puppy | `STRING_ITEM_NAME_2050` | `equipment_barking_puppy` | `charge_affix`=[, %]; `dec_gauge_per_use`=1; `dec_gauge_use_period`=4800; `default_provide_gauge`=100; `equip_type`=6; `hand_weapon_type`=0; `item_upgrade_cost`=150; `item_upgradedid`=2550; `max_durability`=1; `max_gauge`=100; `stat_list`=[]; `use_charge`=true; `use_destroy_by_gauge`=true; `use_item_upgrade`=true; `visible_gauge_count`=true; `weight`=3000 |
| 2500 | Mega flashlight | `STRING_ITEM_NAME_2500` | `equipment_upgraded_flashlight` | `charge_affix`=[, %]; `dec_gauge_initial_only`=1; `dec_gauge_per_use`=2; `dec_gauge_use_period`=4800; `default_provide_gauge`=100; `equip_type`=2; `hand_weapon_type`=0; `max_gauge`=100; `stat_list`=[]; `use_charge`=true; `visible_gauge_count`=true; `weight`=6000 |
| 2510 | Treasure compass | `STRING_ITEM_NAME_2510` | `equipment_treasurehuntcompass` | `dec_gauge_use_period`=60000; `default_provide_gauge`=100; `equip_type`=2; `hand_weapon_type`=0; `max_gauge`=100; `stat_list`=[]; `weight`=6000 |
| 2520 | Rowdy Mother Hen | `STRING_ITEM_NAME_2520` | `equipment_detector_egg` | `equip_type`=4; `hand_weapon_type`=0; `item_upgrade_cost`=10; `item_upgradedid`=1000; `stat_list`=[]; `weight`=3000 |
| 2530 | Paint Gun | `STRING_ITEM_NAME_2530` | `equipment_paintgun` | `charge_affix`=[, %]; `default_provide_gauge`=100; `hand_weapon_type`=1; `max_durability`=210; `max_gauge`=100; `min_durability`=190; `skill_list`=[10845]; `stat_list`=[]; `use_charge`=true; `visible_gauge_count`=true; `weight`=8000 |
| 2531 | STRING_ITEM_NAME_2531 | `STRING_ITEM_NAME_2531` | `equipment_paintgun` | `charge_affix`=[, %]; `default_provide_gauge`=99999; `hand_weapon_type`=1; `max_durability`=99999; `max_gauge`=100; `min_durability`=99999; `skill_list`=[10845]; `stat_list`=[]; `use_charge`=true; `visible_gauge_count`=true; `weight`=1000 |
| 2540 | Stunner | `STRING_ITEM_NAME_2540` | `equipment_electric_field` | `charge_affix`=[, %]; `dec_gauge_initial_only`=2; `dec_gauge_per_use`=1; `dec_gauge_use_period`=667; `default_provide_gauge`=100; `equip_type`=2; `hand_weapon_type`=0; `handheld_abnormal_by_gauge`=true; `handheld_auraskill_by_gauge`=true; `handheld_auraskill_id`=90008; `max_gauge`=100; `min_durability`=100; `use_charge`=true; `visible_gauge_count`=true; `weight`=12000 |
| 2550 | MockHound | `STRING_ITEM_NAME_2550` | `equipment_recording_dog` | `charge_affix`=[, %]; `dec_gauge_per_use`=1; `dec_gauge_use_period`=6000; `default_provide_gauge`=100; `equip_type`=6; `hand_weapon_type`=0; `item_upgradedid`=1000; `max_durability`=1; `max_gauge`=100; `stat_list`=[]; `use_charge`=true; `use_destroy_by_gauge`=true; `visible_gauge_count`=true; `weight`=5000 |
| 2900 | Crowbar | `STRING_ITEM_NAME_2900` | `equipment_crowbar` | `is_two_hand`=true; `max_durability`=50; `min_durability`=20; `price_for_sell_max`=1; `price_for_sell_min`=1; `skill_gauge_on`=[10806, 12900]; `skill_list`=[10806, 12900]; `weight`=10000 |
| 5100 | Wire | `STRING_ITEM_NAME_5100` | `miscellnary_wires` | `max_durability`=10; `min_durability`=7; `price_for_sell_max`=19; `price_for_sell_min`=15; `skill_gauge_on`=[11002, 15100]; `skill_list`=[11002, 15100]; `weight`=12000 |
| 5101 | Torn Umbrella | `STRING_ITEM_NAME_5101` | `miscellnary_brokenumbrella` | `max_durability`=6; `min_durability`=4; `price_for_sell_max`=8; `price_for_sell_min`=6; `skill_gauge_on`=[11002, 15101]; `skill_list`=[11002, 15101]; `use_bonus_item`=true; `weight`=4000 |
| 5102 | Keyboard | `STRING_ITEM_NAME_5102` | `miscellnary_keyboard` | `max_durability`=6; `min_durability`=4; `price_for_sell_max`=4; `price_for_sell_min`=4; `skill_gauge_on`=[11002, 15102]; `skill_list`=[11002, 15102]; `use_bonus_item`=true; `weight`=3000 |
| 5103 | Toilet Paper Roll | `STRING_ITEM_NAME_5103` | `miscellnary_toiletpaper` | `max_durability`=6; `min_durability`=4; `price_for_sell_max`=48; `price_for_sell_min`=40; `skill_gauge_on`=[11001, 15103]; `skill_list`=[11001, 15103]; `weight`=500 |
| 5104 | Rubber Cone | `STRING_ITEM_NAME_5104` | `miscellnary_trafficcone` | `max_durability`=8; `min_durability`=6; `price_for_sell_max`=15; `price_for_sell_min`=13; `skill_gauge_on`=[11002, 15104]; `skill_list`=[11002, 15104]; `use_bonus_item`=true; `weight`=5500 |
| 5105 | Broken Traffic Light | `STRING_ITEM_NAME_5105` | `miscellnary_trafficlight` | `max_durability`=12; `min_durability`=8; `price_for_sell_max`=34; `price_for_sell_min`=28; `skill_gauge_on`=[11003, 15105]; `skill_list`=[11003, 15105]; `weight`=27000 |
| 5106 | Frying Pan | `STRING_ITEM_NAME_5106` | `miscellnary_frypan` | `max_durability`=12; `min_durability`=8; `price_for_sell_max`=42; `price_for_sell_min`=34; `skill_gauge_on`=[11003, 15106]; `skill_list`=[11003, 15106]; `weight`=17000 |
| 5107 | Toilet Seat Cover | `STRING_ITEM_NAME_5107` | `miscellnary_toiletbucket` | `max_durability`=8; `min_durability`=6; `price_for_sell_max`=10; `price_for_sell_min`=8; `skill_gauge_on`=[11002, 15107]; `skill_list`=[11002, 15107]; `use_bonus_item`=true |
| 5110 | Shining Frog | `STRING_ITEM_NAME_5110` | `miscellnary_frog` | `max_durability`=3; `min_durability`=2; `price_for_sell_max`=109; `price_for_sell_min`=89; `skill_gauge_on`=[11001, 15110]; `skill_list`=[11001, 15110]; `weight`=14000 |
| 5111 | Defective Bomb | `STRING_ITEM_NAME_5111` | `miscellnary_timebomb` | `dec_gauge_per_use`=1; `dec_gauge_use_period`=1000; `equip_type`=4; `hand_weapon_type`=0; `is_two_hand`=true; `max_durability`=5; `min_durability`=3; `price_for_sell_max`=219; `price_for_sell_min`=179; `stat_list`=[]; `use_destroy_by_gauge`=true; `weight`=25000 |
| 5113 | Broken Radio | `STRING_ITEM_NAME_5113` | `miscellnary_brokencassette` | `equip_type`=3; `hand_weapon_type`=0; `is_two_hand`=true; `min_durability`=100; `price_for_sell_max`=59; `price_for_sell_min`=49; `weight`=18000 |
| 5117 | Rattle | `STRING_ITEM_NAME_5117` | `miscellnary_rattle` | `max_durability`=8; `min_durability`=6; `price_for_sell_max`=8; `price_for_sell_min`=6; `skill_gauge_on`=[11002, 15117]; `skill_list`=[11002, 15117]; `use_bonus_item`=true; `weight`=3000 |
| 5122 | Cuckoo Clock | `STRING_ITEM_NAME_5122` | `miscellnary_cuckooclock` | `equip_type`=3; `hand_weapon_type`=0; `is_two_hand`=true; `min_durability`=100; `price_for_sell_max`=64; `price_for_sell_min`=52; `weight`=22000 |
| 5123 | lost wallet | `STRING_ITEM_NAME_5123` | `miscellnary_wallet` | `max_durability`=6; `min_durability`=4; `price_for_sell_max`=49; `price_for_sell_min`=21; `skill_gauge_on`=[11001, 15123]; `skill_list`=[11001, 15123]; `weight`=500 |
| 5127 | Music Box | `STRING_ITEM_NAME_5127` | `miscellnary_musicbox` | `max_durability`=8; `min_durability`=6; `price_for_sell_max`=43; `price_for_sell_min`=35; `skill_gauge_on`=[11002, 15127]; `skill_list`=[11002, 15127]; `weight`=7000 |
| 5128 | Toy Airplane | `STRING_ITEM_NAME_5128` | `miscellnary_toyairplane` | `max_durability`=10; `min_durability`=7; `price_for_sell_max`=47; `price_for_sell_min`=39; `skill_gauge_on`=[11002, 11010]; `skill_list`=[11002, 11010]; `weight`=4000 |
| 5130 | Fan | `STRING_ITEM_NAME_5130` | `miscellnary_fan` | `charge_affix`=[, %]; `dec_gauge_initial_only`=5; `dec_gauge_per_use`=1; `dec_gauge_use_period`=3000; `default_provide_gauge`=100; `equip_type`=2; `handheld_abnormal_by_gauge`=true; `handheld_abnormal_id`=10014; `max_gauge`=100; `min_durability`=100; `price_for_sell_max`=14; `price_for_sell_min`=12; `use_bonus_item`=true; `use_charge`=true; `visible_gauge_count`=true; `weight`=16000 |
| 5131 | Megaphone | `STRING_ITEM_NAME_5131` | `miscellnary_loudspeaker` | `charge_affix`=[, %]; `dec_gauge_initial_only`=5; `dec_gauge_per_use`=2; `dec_gauge_use_period`=3000; `default_provide_gauge`=100; `equip_type`=2; `handheld_abnormal_by_gauge`=true; `handheld_abnormal_id`=10013; `max_gauge`=100; `min_durability`=100; `price_for_sell_max`=10; `price_for_sell_min`=8; `use_bonus_item`=true; `use_charge`=true; `visible_gauge_count`=true; `weight`=7000 |
| 5132 | Big Mouth Billy Bass | `STRING_ITEM_NAME_5132` | `miscellnary_billybass` | `equip_type`=3; `hand_weapon_type`=0; `is_two_hand`=true; `min_durability`=100; `price_for_sell_max`=69; `price_for_sell_min`=57; `weight`=16000 |
| 5133 | Pedometer | `STRING_ITEM_NAME_5133` | `miscellnary_pedometer` | `inc_gauge_when_move`=22; `max_durability`=8; `max_gauge`=10000; `min_durability`=6; `overflow_price`=1; `price_for_sell_max`=6; `price_for_sell_min`=5; `price_inc_per_gauge`=1; `skill_list`=[11002]; `weight`=8000 |
| 5134 | Lucky Doll | `STRING_ITEM_NAME_5134` | `miscellnary_voodoodoll` | `blackout_rate`=10000; `max_durability`=6; `min_durability`=4; `price_for_sell_max`=66; `price_for_sell_min`=66; `skill_gauge_on`=[11001, 15134]; `skill_list`=[11001, 15134]; `weight`=3000 |
| 5135 | Portrait | `STRING_ITEM_NAME_5135` | `miscellnary_portrait` | `equip_type`=3; `hand_weapon_type`=0; `is_two_hand`=true; `min_durability`=100; `price_for_sell_max`=80; `price_for_sell_min`=27; `weight`=13000 |
| 5136 | Wall Clock | `STRING_ITEM_NAME_5136` | `miscellnary_clock` | `equip_type`=3; `hand_weapon_type`=0; `is_two_hand`=true; `min_durability`=100; `price_for_sell_max`=35; `price_for_sell_min`=29; `weight`=14000 |
| 5137 | Poop Bag | `STRING_ITEM_NAME_5137` | `miscellnary_poopbag` | `hand_weapon_type`=0; `min_durability`=70; `price_for_sell_max`=66; `price_for_sell_min`=54; `skill_gauge_on`=[0, 15137]; `skill_list`=[0, 15137]; `weight`=22000 |
| 5138 | Owl Statue | `STRING_ITEM_NAME_5138` | `miscellnary_owlstatue` | `hand_weapon_type`=0; `max_durability`=8; `min_durability`=6; `price_for_sell_max`=52; `price_for_sell_min`=42; `skill_gauge_on`=[11002, 15138]; `skill_list`=[11002, 15138]; `weight`=13000 |
| 5139 | Boombox | `STRING_ITEM_NAME_5139` | `miscellnary_boombox` | `equip_type`=3; `hand_weapon_type`=0; `is_two_hand`=true; `min_durability`=100; `price_for_sell_max`=76; `price_for_sell_min`=62; `weight`=25000 |
| 5140 | Mysterious Figurine | `STRING_ITEM_NAME_5140` | `miscellnary_talkingdoll` | `hand_weapon_type`=0; `max_durability`=8; `min_durability`=6; `price_for_sell_max`=34; `price_for_sell_min`=28; `skill_gauge_on`=[11002, 15140]; `skill_list`=[11002, 15140]; `weight`=3000 |
| 5142 | Pinwheel | `STRING_ITEM_NAME_5142` | `miscellnary_pinwheel` | `max_durability`=6; `min_durability`=4; `price_for_sell_max`=11; `price_for_sell_min`=9; `skill_gauge_on`=[11001, 15142]; `skill_list`=[11001, 15142]; `use_bonus_item`=true; `weight`=500 |
| 5143 | Rubber Ducky | `STRING_ITEM_NAME_5143` | `miscellnary_toyduck` | `max_durability`=10; `min_durability`=7; `price_for_sell_max`=28; `price_for_sell_min`=23; `skill_gauge_on`=[11004, 15143]; `skill_list`=[11004, 15143]; `weight`=1000 |
| 5145 | Mug | `STRING_ITEM_NAME_5145` | `miscellnary_mugcup` | `max_durability`=8; `min_durability`=6; `price_for_sell_max`=20; `price_for_sell_min`=16; `skill_gauge_on`=[11002, 15145]; `skill_list`=[11002, 15145]; `weight`=1000 |
| 5146 | Spork | `STRING_ITEM_NAME_5146` | `miscellnary_spork` | `max_durability`=12; `min_durability`=8; `price_for_sell_max`=25; `price_for_sell_min`=21; `skill_gauge_on`=[11003, 15146]; `skill_list`=[11003, 15146]; `weight`=1000 |
| 5147 | Plunger | `STRING_ITEM_NAME_5147` | `miscellnary_plunger` | `max_durability`=8; `min_durability`=6; `price_for_sell_max`=9; `price_for_sell_min`=7; `skill_gauge_on`=[11002, 15147]; `skill_list`=[11002, 15147]; `use_bonus_item`=true |
| 5148 | Quokka Doll | `STRING_ITEM_NAME_5148` | `miscellnary_quokkadoll` | `max_durability`=6; `min_durability`=4; `price_for_sell_max`=32; `price_for_sell_min`=26; `skill_gauge_on`=[11001, 15148]; `skill_list`=[11001, 15148]; `weight`=3000 |
| 5149 | Lighter | `STRING_ITEM_NAME_5149` | `miscellnary_lighter` | `max_durability`=8; `min_durability`=6; `price_for_sell_max`=28; `price_for_sell_min`=23; `skill_gauge_on`=[11002, 15149]; `skill_list`=[11002, 15149]; `weight`=1500 |
| 5151 | Folding Chair | `STRING_ITEM_NAME_5151` | `miscellnary_foldingchair` | `is_two_hand`=true; `max_durability`=8; `min_durability`=6; `price_for_sell_max`=26; `price_for_sell_min`=22; `skill_list`=[10807]; `weight`=12000 |
| 5153 | Whiteboard | `STRING_ITEM_NAME_5153` | `miscellnary_whiteboard_text1` | `equip_type`=3; `hand_weapon_type`=0; `is_two_hand`=true; `min_durability`=100; `price_for_sell_max`=21; `price_for_sell_min`=17; `weight`=10000 |
| 5154 | Whiteboard | `STRING_ITEM_NAME_5154` | `miscellnary_whiteboard_text2` | `equip_type`=3; `hand_weapon_type`=0; `is_two_hand`=true; `min_durability`=100; `price_for_sell_max`=21; `price_for_sell_min`=17; `weight`=10000 |
| 5155 | Whiteboard | `STRING_ITEM_NAME_5155` | `miscellnary_whiteboard_text3` | `equip_type`=3; `hand_weapon_type`=0; `is_two_hand`=true; `min_durability`=100; `price_for_sell_max`=21; `price_for_sell_min`=17; `weight`=10000 |
| 5156 | Whiteboard | `STRING_ITEM_NAME_5156` | `miscellnary_whiteboard_text4` | `equip_type`=3; `hand_weapon_type`=0; `is_two_hand`=true; `min_durability`=100; `price_for_sell_max`=21; `price_for_sell_min`=17; `weight`=10000 |
| 5157 | Whiteboard | `STRING_ITEM_NAME_5157` | `miscellnary_whiteboard_text5` | `equip_type`=3; `hand_weapon_type`=0; `is_two_hand`=true; `min_durability`=100; `price_for_sell_max`=21; `price_for_sell_min`=17; `weight`=10000 |
| 5158 | Old Sneakers | `STRING_ITEM_NAME_5158` | `miscellnary_shoe` | `max_durability`=8; `min_durability`=6; `price_for_sell_max`=7; `price_for_sell_min`=5; `skill_gauge_on`=[11002, 15158]; `skill_list`=[11002, 15158]; `use_bonus_item`=true |
| 5159 | Rotten Bonsai | `STRING_ITEM_NAME_5159` | `miscellnary_rottenbonsai` | `max_durability`=8; `min_durability`=6; `price_for_sell_max`=40; `price_for_sell_min`=32; `skill_gauge_on`=[11002, 15159]; `skill_list`=[11002, 15159]; `weight`=17000 |
| 5160 | Light Stick | `STRING_ITEM_NAME_5160` | `miscellnary_lightstick` | `max_durability`=8; `min_durability`=6; `price_for_sell_max`=47; `price_for_sell_min`=39; `skill_gauge_on`=[11002, 15160]; `skill_list`=[11002, 15160]; `weight`=3000 |
| 5161 | Spray Bottle | `STRING_ITEM_NAME_5161` | `miscellnary_sprayer` | `dec_gauge_per_use`=1; `default_provide_gauge`=5; `hand_weapon_type`=1; `max_gauge`=5; `min_durability`=100; `price_for_sell_max`=7; `price_for_sell_min`=5; `skill_gauge_on`=[11005]; `skill_list`=[11005]; `weight`=5000 |
| 5162 | Cat Wand | `STRING_ITEM_NAME_5162` | `miscellnary_cattoy` | `max_durability`=6; `min_durability`=4; `price_for_sell_max`=31; `price_for_sell_min`=25; `skill_gauge_on`=[11001, 15162]; `skill_list`=[11001, 15162]; `weight`=500 |
| 5167 | Spray Bottle | `STRING_ITEM_NAME_5167` | `miscellnary_sprayer_polluted` | `dec_gauge_per_use`=1; `default_provide_gauge`=5; `hand_weapon_type`=1; `max_gauge`=5; `min_durability`=100; `price_for_sell_max`=7; `price_for_sell_min`=5; `skill_gauge_on`=[11006]; `skill_list`=[11006]; `weight`=5000 |
| 5168 | Warm Poop | `STRING_ITEM_NAME_5168` | `miscellnary_babyrilla_poop` | `max_durability`=1; `price_for_sell_max`=15; `price_for_sell_min`=5; `skill_gauge_on`=[11002, 15168]; `skill_list`=[11002, 15168] |
| 5169 | Golden Poop | `STRING_ITEM_NAME_5169` | `miscellnary_babyrilla_goldenpoop` | `max_durability`=1; `price_for_sell_max`=200; `price_for_sell_min`=100; `skill_gauge_on`=[11002, 15169]; `skill_list`=[11002, 15169] |
| 6000 | Key | `STRING_ITEM_NAME_6000` | `miscellnary_key` | `max_durability`=10; `min_durability`=10; `skill_list`=[11001]; `weight`=500 |
| 8888 | Unknown Black Matter | `STRING_ITEM_NAME_8888` | `miscellnary_unknown_black_matter` | `max_durability`=6; `min_durability`=4; `price_for_sell_max`=1; `price_for_sell_min`=1; `skill_gauge_on`=[11007, 18888]; `skill_list`=[11007, 18888]; `weight`=100 |
| 10000 | Golden Rattle | `STRING_ITEM_NAME_10000` | `miscellnary_rattle_dl_test` | `max_durability`=99999; `min_durability`=99999; `price_for_sell_max`=9999; `price_for_sell_min`=9999; `skill_gauge_on`=[11007, 19117]; `skill_list`=[11007, 19117]; `weight`=100 |
| 59998 | Projectile Physics Test | `STRING_ITEM_NAME_59998` | `equipment_shotgun` | `dec_gauge_per_use`=1; `default_provide_gauge`=9999; `hand_weapon_type`=1; `max_gauge`=9999; `skill_list`=[19998]; `skill_reload`=10901; `visible_gauge_count`=true; `weight`=4000 |
| 59999 | Projectile Ray Test | `STRING_ITEM_NAME_59999` | `equipment_shotgun` | `dec_gauge_per_use`=1; `default_provide_gauge`=9999; `hand_weapon_type`=1; `max_gauge`=9999; `skill_list`=[19999]; `skill_reload`=10901; `visible_gauge_count`=true; `weight`=4000 |
| 772030 | 페인트 볼 짭(테스트용) | `페인트 볼 짭(테스트용)` | `equipment_paintball` | `default_provide_gauge`=77; `max_durability`=77; `max_gauge`=77; `min_durability`=77; `skill_gauge_on`=[10841]; `skill_list`=[10841]; `stat_list`=[]; `visible_gauge_count`=true; `weight`=100 |
| 775128 | Toy Airplane | `STRING_ITEM_NAME_5128` | `miscellnary_toyairplane` | `max_durability`=10; `min_durability`=5; `price_for_sell_max`=47; `price_for_sell_min`=39; `skill_gauge_on`=[11011]; `skill_list`=[11011]; `visible_durability_count`=true; `weight`=4000 |
| 775161 | 분무기 오염도 낮추기(테스트용) | `분무기 오염도 낮추기(테스트용)` | `miscellnary_sprayer` | `default_provide_gauge`=100; `max_gauge`=100; `min_durability`=100; `price_for_sell_max`=9; `price_for_sell_min`=3; `skill_gauge_on`=[11005]; `skill_list`=[11005]; `visible_gauge_count`=true; `weight`=5000 |
| 777777 | One-Hit Bat for DL Request | `STRING_ITEM_NAME_777777` | `equipment_woodbat_dl_test` | `is_two_hand`=true; `skill_list`=[17777] |
| 785161 | 분무기 오염도 올리기(테스트용) | `분무기 오염도 올리기(테스트용)` | `miscellnary_sprayer` | `default_provide_gauge`=100; `max_gauge`=100; `min_durability`=100; `price_for_sell_max`=9; `price_for_sell_min`=3; `skill_gauge_on`=[11006]; `skill_list`=[11006]; `visible_gauge_count`=true; `weight`=5000 |
| 991000 | Shotgun | `STRING_ITEM_NAME_1000` | `equipment_shotgun` | `dec_gauge_per_use`=1; `hand_weapon_type`=1; `handheld_abnormal_by_gauge`=true; `handheld_auraskill_by_gauge`=true; `is_promotion_item`=true; `max_durability`=50; `max_gauge`=1; `min_durability`=30; `price_for_sell_max`=2; `price_for_sell_min`=2; `skill_list`=[10900]; `skill_reload`=10901; `visible_gauge_count`=true; `weight`=4000 |

## Miscellany (38)

| Master ID | English name | Name key | Loot prefab | Key properties |
|-----------|--------------|----------|-------------|----------------|
| 5000 | Empty Glass Bottle | `STRING_ITEM_NAME_5000` | `garb_bottle_01` | `price_for_sell_max`=11; `price_for_sell_min`=11 |
| 5001 | Empty Plastic Bottle | `STRING_ITEM_NAME_5001` | `garb_bottle_02` | `price_for_sell_max`=8; `price_for_sell_min`=8; `weight`=3000 |
| 5002 | Spare Tire | `STRING_ITEM_NAME_5002` | `garb_Tire` | `price_for_sell_max`=37; `price_for_sell_min`=37; `weight`=43000 |
| 5003 | Truck Engine | `STRING_ITEM_NAME_5003` | `engine_big_truck` | `price_for_sell_max`=80; `price_for_sell_min`=80; `weight`=62000 |
| 5004 | Empty Gas Canister | `STRING_ITEM_NAME_5004` | `gas_tank_empty` | `price_for_sell_max`=7; `price_for_sell_min`=7; `weight`=20000 |
| 5005 | Half-Full Gas Canister | `STRING_ITEM_NAME_5005` | `gas_tank_half` | `price_for_sell_max`=23; `price_for_sell_min`=23; `weight`=21000 |
| 5006 | Full Gas Canister | `STRING_ITEM_NAME_5006` | `gas_tank_full` | `price_for_sell_max`=44; `price_for_sell_min`=44; `weight`=21000 |
| 5007 | Gasoline Can | `STRING_ITEM_NAME_5007` | `gasoline_can` | `price_for_sell_max`=57; `price_for_sell_min`=57; `weight`=14000 |
| 5008 | Wastewater Drum | `STRING_ITEM_NAME_5008` | `wastewater_can` | `price_for_sell_max`=17; `price_for_sell_min`=17; `weight`=12000 |
| 5009 | Beer Bottle | `STRING_ITEM_NAME_5009` | `beer_bottle` | `price_for_sell_max`=22; `price_for_sell_min`=22; `weight`=6000 |
| 5010 | Pepsi Zero Lime | `STRING_ITEM_NAME_5010` | `pepsi_zero_lime_bottle` | `price_for_sell_max`=16; `price_for_sell_min`=16; `weight`=4000 |
| 5011 | Toothed Gear | `STRING_ITEM_NAME_5011` | `machine_component` | `price_for_sell_max`=150; `price_for_sell_min`=150; `weight`=10000 |
| 5012 | Gol-Den Oxygen Tank (Empty) | `STRING_ITEM_NAME_5012` | `oxygen_bottle_golden` | `price_for_sell_max`=199; `price_for_sell_min`=199; `weight`=50000 |
| 5108 | Dead Battery | `STRING_ITEM_NAME_5108` | `miscellnary_carbattery` | `deteriorate_item`=true; `forbid_change`=true; `price_for_sell_max`=45; `price_for_sell_min`=37; `weight`=24000 |
| 5109 | Dummy | `STRING_ITEM_NAME_5109` | `miscellnary_dummybust` | `deteriorate_item`=true; `forbid_change`=true; `price_for_sell_max`=53; `price_for_sell_min`=43; `weight`=28000 |
| 5114 | Old Tire | `STRING_ITEM_NAME_5114` | `miscellnary_wastetire` | `deteriorate_item`=true; `forbid_change`=true; `price_for_sell_max`=53; `price_for_sell_min`=43; `weight`=38000 |
| 5116 | Dumbbell | `STRING_ITEM_NAME_5116` | `miscellnary_dumbbell` | `deteriorate_item`=true; `forbid_change`=true; `price_for_sell_max`=40; `price_for_sell_min`=32; `weight`=30000 |
| 5120 | Guitar | `STRING_ITEM_NAME_5120` | `miscellnary_guitar` | `deteriorate_item`=true; `price_for_sell_max`=8; `price_for_sell_min`=6; `use_bonus_item`=true; `weight`=7000 |
| 5124 | Golden Statue | `STRING_ITEM_NAME_5124` | `miscellnary_goldenstatue` | `deteriorate_item`=true; `forbid_change`=true; `price_for_sell_max`=164; `price_for_sell_min`=134; `weight`=45000 |
| 5141 | Cardboard Car | `STRING_ITEM_NAME_5141` | `miscellnary_boxcar` | `deteriorate_item`=true; `forbid_change`=true; `price_for_sell_max`=30; `price_for_sell_min`=24; `weight`=11000 |
| 5150 | Inflatable T-Rex | `STRING_ITEM_NAME_5150` | `miscellnary_dinosaurairsuit` | `deteriorate_item`=true; `forbid_change`=true; `price_for_sell_max`=45; `price_for_sell_min`=37 |
| 5163 | Gold Chain | `STRING_ITEM_NAME_5163` | `miscellnary_blingblingchain` | `accessory_group`=3; `deteriorate_item`=true; `price_for_sell_max`=80; `price_for_sell_min`=66; `weight`=12000 |
| 5164 | Petal | `STRING_ITEM_NAME_5164` | `miscellnary_petalhat` | `accessory_group`=4; `deteriorate_item`=true; `price_for_sell_max`=17; `price_for_sell_min`=14; `weight`=1000 |
| 5165 | Gat | `STRING_ITEM_NAME_5165` | `miscellnary_gat` | `accessory_group`=1; `use_vending_machine_exchange`=false; `weight`=1000 |
| 5166 | Cardboard Plane | `STRING_ITEM_NAME_5166` | `miscellnary_boxairplane` | `deteriorate_item`=true; `forbid_change`=true; `price_for_sell_max`=24; `price_for_sell_min`=20; `weight`=11000 |
| 5924 | Golden Statue | `STRING_ITEM_NAME_5124` | `miscellnary_goldenstatue` | `forbid_change`=true; `price_for_sell_max`=219; `price_for_sell_min`=179; `weight`=45000 |
| 5995 | Frog Hat | `STRING_ITEM_NAME_5995` | `miscellnary_frogheadgear` | `accessory_group`=1; `use_vending_machine_exchange`=false |
| 5996 | Chicken Hat | `STRING_ITEM_NAME_5996` | `miscellnary_chickenheadgear` | `accessory_group`=1; `use_vending_machine_exchange`=false |
| 5997 | Pumpkin Hat | `STRING_ITEM_NAME_5997` | `miscellnary_pumpkinheadgear` | `accessory_group`=1; `use_vending_machine_exchange`=false |
| 5998 | Stylish cap | `STRING_ITEM_NAME_5998` | `miscellnary_hiphat` | `accessory_group`=1; `use_vending_machine_exchange`=false |
| 5999 | Sunglasses | `STRING_ITEM_NAME_5999` | `miscellnary_sunglass` | `accessory_group`=2; `use_vending_machine_exchange`=false |
| 8114 | Old Tire | `STRING_ITEM_NAME_5114` | `miscellnary_guitar` | `forbid_change`=true; `price_for_sell_max`=29; `price_for_sell_min`=25; `weight`=27000 |
| 8116 | Dumbbell | `STRING_ITEM_NAME_5116` | `miscellnary_guitar` | `price_for_sell_max`=43; `price_for_sell_min`=35; `weight`=30000 |
| 9116 | Dumbbell | `STRING_ITEM_NAME_5116` | `miscellnary_dumbbell` | `forbid_change`=true; `price_for_sell_max`=43; `price_for_sell_min`=35; `weight`=30000 |
| 9122 | Cuckoo Clock | `STRING_ITEM_NAME_5122` | `miscellnary_cuckooclock` | `forbid_change`=true; `price_for_sell_max`=21; `price_for_sell_min`=17; `weight`=9000 |
| 9124 | Golden Statue | `STRING_ITEM_NAME_5124` | `miscellnary_goldenstatue` | `forbid_change`=true; `price_for_sell_max`=219; `price_for_sell_min`=179; `weight`=45000 |
| 9999 | Test Item | `STRING_ITEM_NAME_9999` | `gasoline_can` | `price_for_sell_max`=9999; `price_for_sell_min`=9999; `weight`=0 |
| 995108 | Dead Battery | `STRING_ITEM_NAME_5108` | `miscellnary_carbattery` | `forbid_change`=true; `is_promotion_item`=true; `price_for_sell_max`=0; `price_for_sell_min`=0; `weight`=24000 |
