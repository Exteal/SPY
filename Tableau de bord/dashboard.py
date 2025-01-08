import os
import pandas as pd
import dash
from dash import dcc, html
from dash.dependencies import Input, Output
import data_processing
import graphs
import plotly.graph_objects as go
import utils
#import lrs_fetch


# Initialize the Dash app with suppressing callback exceptions
app = dash.Dash(__name__, suppress_callback_exceptions=True)

input_folder = 'traces'
output_folder = 'processed_data'
#lrs_fetch.request_data(input_folder)
data_processing.process_csv_files(input_folder, output_folder)

# Load model data
model_file = 'model.csv'
model_df = pd.read_csv(model_file)

player_data = {}


# Read CSV files for each player
for file_name in os.listdir(output_folder):
    if file_name.endswith('.csv'):
        player_id = file_name.replace('.csv', '')
        file_path = os.path.join(output_folder, file_name)
        df = pd.read_csv(file_path)

        # Calculate time spent
        time_spent = utils.calculate_time_spent(df)

        player_data[player_id] = {
            'scenarios': df['scenario'].unique().tolist(),
            'data': df,
            'time_spent': time_spent,
        }

        # expertise
        loops_exp, conditions_exp = utils.calculate_expertise(player_data[player_id]['time_spent'], model_df)        
        player_data[player_id]['expertise'] = {
            'loops': loops_exp,
            'conditions': conditions_exp
        }

# Calculer les taux d'accomplissement
completion_df = graphs.calculate_completion_rate(player_data, model_file)

# Dash layout
app.layout = html.Div([
    html.H1("Spy Dashboard"),
    html.Div([
        html.Div([
            html.H2("Players"),
            html.Div(id='player-list', className="scrollable-list"),
        ], className="left-panel"),
        html.Div([
            html.Div(id='player-details', className="detail-box"),
            html.Div(id='scenario-details', className="detail-box"),
        ], className="right-panel"),
        html.Div([
            dcc.Graph(id="expertise-graph", style={"height": "150px"})
        ], style={
            "position": "fixed",
            "left": "50%", 
            "right": "25%",           
            "marginTop": "80px",
            "padding": "20px"
            }),
        html.Div([
            dcc.Graph(id="completion-pie-chart", style={"height": "150px"})
        ], style={
            "position": "fixed",
            "left": "73%", 
            "right": "10px",           
            "marginTop": "80px",
            "padding": "20px"
        }),
        html.Div([
            dcc.Graph(id="scenario-graph", style={"height": "300px"}),  
        ], style={
            "position": "fixed",
            "right": "10px",
            "left": "50%",            
            "marginTop": "250px",
            "padding": "20px"
            }),
    ], style={"display": "flex", "width": "90%", "height": "80%", "margin": "auto"}),
    dcc.Store(id='selected-player', data=None),
    dcc.Store(id='selected-scenario', data=None),
])


# Update the list of players
@app.callback(
    Output('player-list', 'children'),
    [Input('selected-player', 'data')]
)
def display_player_list(selected_player):
    ranked_players = utils.ranking(player_data)
    return [
        html.Div(
            f"{player_id} {icon}",
            id=f'player-{player_id}',
            n_clicks=0,
            className="player-item selected" if player_id == selected_player else "player-item"
        )
        for player_id, _, icon in ranked_players
    ]


# Update selected player in the store
@app.callback(
    Output('selected-player', 'data'),
    [Input(f'player-{player}', 'n_clicks') for player in player_data.keys()]
)
def update_selected_player(*n_clicks):
    ctx = dash.callback_context
    if not ctx.triggered:
        return None
    triggered_id = ctx.triggered[0]['prop_id'].split('.')[0]
    
    # Extract the player ID from the triggered component
    if triggered_id.startswith('player-'):
        return triggered_id.replace('player-', '')
    return None


# Display player details based on selected player
@app.callback(
    Output('player-details', 'children'),
    [Input('selected-player', 'data'), Input('selected-scenario', 'data')]
)
def display_player_details(selected_player, selected_scenario):
    if not selected_player or selected_player not in player_data:
        return html.Div("Select a player to view details.")

    player_info = player_data[selected_player]
    played_scenarios = set(player_info['scenarios'])
    scenarios = model_df['scenario'].unique()
    return [
        html.Div(
            scenario,
            id=f'scenario-{scenario}',
            className=(
                "scenario-item scenario-item-played selected"
                if scenario == selected_scenario
                else "scenario-item scenario-item-played"
                if scenario in played_scenarios
                else "scenario-item scenario-item-unplayed"
            )
        )
        for scenario in scenarios
    ]


# Update selected scenario in the store
@app.callback(
    Output('selected-scenario', 'data'),
    [Input(f'scenario-{scenario}', 'n_clicks') for scenario in model_df['scenario'].unique()]
)
def update_selected_scenario(*n_clicks):
    ctx = dash.callback_context
    if not ctx.triggered:
        return None
    triggered_id = ctx.triggered[0]['prop_id'].split('.')[0]

    if triggered_id.startswith('scenario-'):
        return triggered_id.replace('scenario-', '')
    return None


@app.callback(
    Output('scenario-details', 'children'),
    [Input('selected-player', 'data'), Input('selected-scenario', 'data')]
)
def display_scenario_details(selected_player, selected_scenario):
    if not selected_player or not selected_scenario:
        return html.Div()

    player_info = player_data[selected_player]
    player_df = player_info['data']
    time_spent_df = player_info['time_spent']

    # Filter the model data for the selected scenario
    scenario_model = model_df[model_df['scenario'] == selected_scenario]

    # Check which levels the player has played
    played_levels = player_df[player_df['scenario'] == selected_scenario]['level'].tolist()

    details = []
    for _, row in scenario_model.iterrows():
        level = row['level']
        
        # Filter time_spent_df for the current level
        time_spent_row = time_spent_df[
            (time_spent_df['scenario'] == selected_scenario) & (time_spent_df['level'] == level)
        ]
        #print("/n",time_spent_row)
        score = 0000.0
        stars = "☆☆☆"
        # Extract and format time spent
        if not time_spent_row.empty:
            score = time_spent_row['score'].max()
            model_scores = model_df[(model_df['scenario'] == selected_scenario) & (model_df['level'] == level)]
            twoStars = model_scores['twoStars'].values[0]
            threeStars = model_scores['score_max'].values[0]
            if score >= twoStars: stars = "★★☆"  
            if score >= threeStars: stars = "★★★"  
            max_score_row = time_spent_row[time_spent_row['score'] == score].values[0]
            #print("max",max_score_row)
            time_spent = max_score_row[2] #index of time
            if pd.notna(time_spent):  # Check if time_spent is not NaT
                # Convert numpy.timedelta64 to a timedelta object and then get total seconds
                time_spent_seconds = pd.to_timedelta(time_spent).total_seconds()
                time_spent_str = str(pd.to_timedelta(time_spent_seconds, unit='s')).split()[2]  # HH:MM:SS
            else:
                time_spent_str = "00:00:00"
        else:
            time_spent_str = "00:00:00"

        details.append(html.Div(
            f" {level} \t\t\t {stars} \t\t\t {time_spent_str}",
            className="level-item level-item-played" if level in played_levels else "level-item level-item-unplayed"
        ))

    return details

@app.callback(
    Output('scenario-graph', 'figure'),
    [Input('selected-scenario', 'data')]
)
def display_scenario_graph(selected_scenario):
    if selected_scenario is None:
        return go.Figure()
    
    # Calculate average time spent across all players
    avg_time_spent_df = graphs.calculate_avg_time_spent(player_data)
    #print(avg_time_spent_df)

    # Plot the graph for the selected scenario
    fig = graphs.plot_time_spent_graph(avg_time_spent_df, selected_scenario)

    return fig

@app.callback(
    Output('expertise-graph', 'figure'),
    [Input('selected-player', 'data')]
)
def update_expertise_graph(selected_player):
    if not selected_player or selected_player not in player_data:
        # Return an empty figure if no player is selected
        return go.Figure()

    fig = graphs.plot_expertise(selected_player, player_data)

    return fig

@app.callback(
    Output('completion-pie-chart', 'figure'),
    [Input('selected-scenario', 'data')]
)
def update_completion_pie_chart(selected_scenario):
    if not selected_scenario:
        return go.Figure()  # Retourne une figure vide si aucun scénario n'est sélectionné

    # Créer le graphique circulaire
    fig = graphs.plot_completion_pie_chart(completion_df, selected_scenario)
    return fig


# Run the Dash app
if __name__ == '__main__':
    app.run_server(debug=True)
