
import pandas as pd

def calculate_time_spent(df):

    '''''
    Calcule le temps passé par niveau en comparant les événements "launched" et "completed".

    '''''
    time_spent_records = []

    for index, row in df[df['verb'] == 'launched'].iterrows():
        scenario = row['scenario']
        level = row['level']
        launch_time = pd.to_datetime(row['time'])

        # Find the next completed event
        subsequent_events = df[(df['time'] > row['time']) & (df['verb'] == 'completed')]
        if not subsequent_events.empty:
            score = subsequent_events.iloc[0]['score']
            completed_time = pd.to_datetime(subsequent_events.iloc[0]['time'])
            time_spent = completed_time - launch_time

            # Add the time spent to the records
            time_spent_records.append({
                'scenario': scenario,
                'level': level,
                'time_spent': time_spent,
                'score': score
            })
    
    return pd.DataFrame(time_spent_records)


def calculate_expertise(df, model_df):
    '''''
    Calcule le niveau d'expertise du joueur en boucles et conditions en fonction des scores.

    '''''    
    # Filter levels with loops or conditions
    levels_with_loops = model_df[model_df['loops'] > 0]
    levels_with_conditions = model_df[model_df['conditions'] > 0]
    max_loops_score = levels_with_loops['score_max'].sum()
    max_conditions_score = levels_with_conditions['score_max'].sum()
    player_loops_completed = 0.0
    player_conditions_completed = 0.0
    
    # Check each level played by the player
    unique_levels = df[['level', 'scenario']].drop_duplicates()

    for _, row in unique_levels.iterrows():        
        level = row['level']
        scenario = row['scenario']
        score = float(df[(df['level'] == level) & (df['scenario'] == scenario)]['score'].max() or 0)        
        # Look up the level in the model data
        level_info = model_df[(model_df['level'] == level) & (model_df['scenario'] == scenario)]
        if not level_info.empty:
            model_level = level_info.iloc[0]
            if model_level['loops'] > 0:
                player_loops_completed += min(score, model_level['score_max'])
            if model_level['conditions'] > 0:
                player_conditions_completed += min(score, model_level['score_max'])

    # Calculate percentages
    loops_expertise = (player_loops_completed / max_loops_score) * 100 if max_loops_score > 0 else 0
    conditions_expertise = (player_conditions_completed / max_conditions_score) * 100 if max_conditions_score > 0 else 0

    return loops_expertise, conditions_expertise


def ranking(player_data):
    '''''
    Classe les joueurs par score total et ajoute des icônes pour les trois premiers.

    '''''
    player_scores = []

    for player_id, player_info in player_data.items():
        # Calculer le score total pour le joueur
        if 'time_spent' in player_info:
            total_score = player_info['time_spent']['score'].sum()
            player_scores.append((player_id, total_score))

    # Trier les joueurs par score décroissant
    player_scores.sort(key=lambda x: x[1], reverse=True)

    # Ajouter des icônes pour les trois premiers joueurs
    icons = ['👑', '🥈', '🥉']
    ranked_players = [
        (player_id, total_score, icons[i]) if i < len(icons) else (player_id, total_score, '')
        for i, (player_id, total_score) in enumerate(player_scores)
    ]

    return ranked_players