import pandas as pd
import csv
import json
import matplotlib.pyplot as plt
import plotly.express as px
import plotly.graph_objects as go



def calculate_avg_time_spent(player_data):
    # Create a list to store aggregated time spent data
    time_spent_records = []
    
    # Loop over each player data
    for player_id, player_info in player_data.items():
        time_spent_df = player_info['time_spent']
        
        # Add the time spent for each player into the records
        for _, row in time_spent_df.iterrows():
            time_spent_records.append({
                'scenario': row['scenario'],
                'level': row['level'],
                'time_spent': row['time_spent'],
                'score': row['score']
            })
        # Convert to DataFrame
        time_spent_records_df = pd.DataFrame(time_spent_records)

        # Handle duplicates: for each (scenario, level), keep the record with the highest score
        time_spent_records_df = time_spent_records_df.sort_values('score', ascending=False).drop_duplicates(subset=['scenario', 'level'], keep='first')
    
    # Convert to DataFrame
    time_spent_df = pd.DataFrame(time_spent_records_df)

    # Group by scenario and level, and calculate the average time spent
    avg_time_spent_df = time_spent_df.groupby(['scenario', 'level']).agg({'time_spent': 'mean'}).reset_index()

    return avg_time_spent_df



def calculate_completion_rate(player_data, model_file):
    """
    Calcule le taux moyen d'accomplissement par scénario pour tous les joueurs.
    """
    # Charger les données du modèle pour obtenir les niveaux disponibles
    model_df = pd.read_csv(model_file)
    total_levels_by_scenario = model_df.groupby('scenario')['level'].nunique().reset_index()
    total_levels_by_scenario.columns = ['scenario', 'total_levels']

    # Initialiser un dictionnaire pour stocker les accomplissements par scénario
    levels_completed = {scenario: [] for scenario in total_levels_by_scenario['scenario']}

    # Parcourir les données des joueurs pour collecter les niveaux accomplis
    for player_id, player_info in player_data.items():
        time_spent_df = player_info['time_spent']
        # Récupérer les scénarios et niveaux accomplis
        completed_levels = time_spent_df[['scenario', 'level']].drop_duplicates()
        for scenario, group in completed_levels.groupby('scenario'):
            levels_completed[scenario].append(len(group['level'].unique()))

    # Calculer la moyenne des niveaux accomplis par scénario
    avg_completed_by_scenario = []
    for scenario, completed_counts in levels_completed.items():
        avg_completed = sum(completed_counts) / len(player_data) 
        total_levels = total_levels_by_scenario.loc[total_levels_by_scenario['scenario'] == scenario, 'total_levels'].values[0]
        avg_completed_by_scenario.append({'scenario': scenario, 'avg_completed': avg_completed, 'total_levels': total_levels})

    # Convertir en DataFrame
    completion_df = pd.DataFrame(avg_completed_by_scenario)
    completion_df['completion_rate'] = (completion_df['avg_completed'] / completion_df['total_levels']) * 100
    return completion_df


def plot_completion_pie_chart(completion_df, selected_scenario):
    """
    Affiche un graphique circulaire du taux d'accomplissement moyen pour un scénario sélectionné.
    """
    # Extraire les données pour le scénario sélectionné
    taux = completion_df[completion_df['scenario'] == selected_scenario]

    if taux.empty:
        return go.Figure()  # Retourne une figure vide si aucune donnée n'est disponible

    # Créer le graphique circulaire
    fig = go.Figure(data=[go.Pie(
        labels=['Accompli', 'Non accompli'],
        values=[taux['completion_rate'].values[0], 100 - taux['completion_rate'].values[0]],
        hole=0.1,  # Donut style
        marker=dict(colors=['rgba(0, 255, 255, 0.7)', 'black'])
    )])

    # Mise en page du graphique
    fig.update_layout(
        title="Taux d'accomplissement moyen",
        height=150,
        autosize=True,
        margin=dict(l=50, r=20, t=40, b=20),
        plot_bgcolor="rgba(0, 0, 0, 0.3)",  
        paper_bgcolor="rgba(240, 240, 240, 0.3)",  
        font=dict(color="white"), 
        annotations=[
            dict(text=f"{selected_scenario}", x=2, y=0, font_size=10, showarrow=False)
        ]
    )

    return fig


def plot_time_spent_graph(avg_time_spent_df, scenario):
    # Filter data for the selected scenario
    scenario_data = avg_time_spent_df[avg_time_spent_df['scenario'] == scenario]

    # Create a Plotly line plot
    fig = px.histogram(
        scenario_data,
        x='level',
        y='time_spent',
        nbins=len(scenario_data['level'].unique()),  # Un bin par niveau
    )
    fig.update_traces(marker_color='rgba(0, 255, 255, 0.7)'  # Bin color
                      )
    fig.update_layout(
        title=f"Temps moyen par niveau - scénario {scenario}",
        xaxis_title="Niveau",
        yaxis_title="Temps moyen passé",
        plot_bgcolor="rgba(0, 0, 0, 0.9)",  
        paper_bgcolor="rgba(240, 240, 240, 0.3)",  
        font=dict(color="white"), 
    )
    return fig

def plot_expertise(selected_player, player_data):
    # Retrieve the expertise data for the selected player
    expertise = player_data[selected_player]['expertise']
    loops_exp = expertise['loops']
    conditions_exp = expertise['conditions']

    # Create a bar chart for expertise
    fig = go.Figure()
    fig.add_trace(go.Bar(
        x=[loops_exp],  # Expertise level
        y=["Loops  "],    # Expertise type
        orientation='h',
        name="Loops",
        marker_color='rgba(0, 255, 255, 0.7)',
        marker_line=dict(
            color='black',  
            width=2  
        )
    ))
    fig.add_trace(go.Bar(
        x=[conditions_exp],  # Expertise level
        y=["Conditions  "],    # Expertise type
        orientation='h',
        name="Conditions",
        marker_color='rgba(0, 255, 255, 0.7)',
        marker_line=dict(
            color='black',  
            width=2  
        )
    ))

    # Style the graph
    fig.update_layout(
        title="Expertise joueur",
        xaxis=dict(title="% d'expertise", range=[0, 100]),
        yaxis=dict(title="", tickfont=dict(size=12)),
        margin=dict(l=50, r=20, t=40, b=30),
        height=150,
        showlegend=False,
        plot_bgcolor="rgba(0, 0, 0, 0.5)",  
        paper_bgcolor="rgba(240, 240, 240, 0.3)",  
        font=dict(color="white"), 
    )
    return fig


