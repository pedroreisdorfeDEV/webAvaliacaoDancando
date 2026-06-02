alter table public.apresentacoes
    add column if not exists audio_parecer_1_path text,
    add column if not exists audio_parecer_2_path text,
    add column if not exists audio_parecer_3_path text;
