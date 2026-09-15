ent-BaseInducer = indutor
    .desc = Um dispositivo para transferir energia sem fio de uma bateria para outros dispositivos. Tem um mecanismo de segurança que previne ele de transferir energia para todo tipo de equipamento de segurança produzido em massa.

ent-InducerEngineering = { ent-BaseInducer }
    .desc = { ent-BaseInducer.desc }
    .suffix = Vazio, Engenharia

ent-InducerEngineeringBattery = { ent-BaseInducer }
    .desc = { ent-BaseInducer.desc }
    .suffix = Bateria, Engenharia

ent-InducerScientific = { ent-BaseInducer }
    .desc = { ent-BaseInducer.desc }
    .suffix = Vazio, P&D

ent-InducerScientificBattery = { ent-BaseInducer }
    .desc = { ent-BaseInducer.desc }
    .suffix = Bateria, P&D

ent-InducerEngineeringWhite = { ent-BaseInducer }
    .desc = { ent-BaseInducer.desc }
    .suffix = Vazio, Engenheiro Chefe

ent-InducerEngineeringWhiteBattery = { ent-BaseInducer }
    .desc = { ent-BaseInducer.desc }
    .suffix = Bateria, Engenheiro Chefe

ent-InducerCombat = indutor de combate
    .desc = Um dispositivo para transferir energia sem fio de uma bateria para outros dispositivos. Essa versão é feita para ser compátivel com armas laser, para recarregamento em combate.
    .suffix = Vazio, Combate

ent-InducerCombatBattery = { ent-InducerCombat }
    .desc = { ent-InducerCombat.desc }
    .suffix = Bateria, Combate

ent-InducerSec = indutor de combate
    .desc = { ent-InducerCombat.desc }
    .suffix = Vazio, Sec

ent-InducerSecBattery = indutor de combate
    .desc = { ent-InducerCombat.desc }
    .suffix = Bateria, Sec
