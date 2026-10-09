# Binary skill states

The game classifies 59 owner/key combinations as named active/inactive states rather than numerical stacks. This is an explicit semantic list in `SkillMechanics.stateNames`; reaching a maximum of 1 alone does not turn an object into a state.

Forms include Irem's cat transformation, Rio's longbow posture, Silvia's bike, Estelle's shield defense, Jenny's Black Tea role, Debi/Marlene's red posture, Blair's blade form, Arda's awakening, and Alex's infiltration. Preparations include Nicky's guard, Isaac's reinforcement, Aiden's completed overcharge, Camilo's alternating actions, Sua's nourishment, Abigail's blade preparation, Blair's follow-up preparations, Lucia's glorious romance, and Ceres's piercing follow-up. Attached target marks and active fields are also binary states.

Physical resources keep numerical values: moving charms, bookmarks, Emma's magical objects, Semtex bombs, Mai's pin, chess pieces, ring ropes, censers, glass swords, amplification screens, logs, time-control devices, teddy bears, and crystals. Resources with a maximum above 1 retain their existing numerical rules, including Nia's arcade blocks and battery.

`state_on`, `state_off`, and `state_toggle` are separate engine operations. Irem R, Rio Q, Estelle E, and Jenny E use one explicit toggle, evaluated against the snapshot before the card. Reapplying an active preparation keeps it active. Hit conditions and shared character ownership are unchanged. Target states say that they are applied to the enemy; transformations say that the actor enters the state; other preparations say that the state becomes active.

Active states appear as `kind="state"` tokens with a label such as `고양이 상태`. They have no numerical denominator or invented turn duration. Inactive states are omitted. Conditions are presented as `고양이 상태이면` or `고양이 상태가 아니면`; numerical resource conditions still include the actual value.

Existing saves preserve their resource owner, key, amount, and cap. `Ensure` recognizes older cap-1 values, restores the `isState` metadata, and clamps them to inactive/active. Cloning preserves the metadata and isolates live state from enemy forecast simulations. Battles retain their existing reset rules.

Full control descriptions also use active/inactive state conditions for Nicky E, three Rio R rules, Magnus E, Debi/Marlene E, Lucia Q, and Ceres Q. Numerical thresholds such as Nia's arcade blocks keep their original values.

Validation: `Tools/RunCoreTests.ps1` passed 17,528 assertions across 45 game-rule scenarios. The added scenario checks repeated Irem transformations and form-dependent Q/W effects, other reversible forms, state reapplication, legacy-save migration, independent forecast clones, physical resource exceptions, globally triggered passive preparations, state descriptions, all eight authored control conditions using named states, and absence of mixed numerical/state ownership keys. Log: `Temp/BinaryStatesCoreTests.log`.
