import os
import csv
import json
import pandas as pd
import re

def process_csv_files(input_folder, output_folder):
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)
    
    # Helper function to safely load JSON
    def safe_json_load(json_string, default={}):
        try:
            return json.loads(json_string.replace("True", "true").replace("'", '"'))
        except (json.JSONDecodeError, TypeError):
            return default

    # Define the fields to extract
    extracted_rows = []
    fieldnames = ['actor', 'scenario', 'level', 'score', 'time', 'verb']

    # Iterate over each file in the folder
    for file_name in os.listdir(input_folder):
        if file_name.endswith('.csv'):
            file_path = os.path.join(input_folder, file_name)
            try:
                with open(file_path, 'r', encoding='utf-8') as csv_file:
                    reader = csv.DictReader(csv_file)
                    # Extract rows with specified fields
                    for row in reader:
                        try:
                            actor = safe_json_load(row.get('actor', '{}')).get('name', '') if row.get('actor') else ''
                            object_data = safe_json_load(row.get('object', '{}'))
                            result_data = safe_json_load(row.get('result', '{}'))
                            scenario = object_data.get('definition', {}).get('extensions', {}).get('https://spy.lip6.fr/xapi/extensions/context', [''])[0]
                            level = object_data.get('definition', {}).get('extensions', {}).get('https://w3id.org/xapi/seriousgames/extensions/progress', [''])[0]
                            score = result_data.get('extensions', {}).get('https://spy.lip6.fr/xapi/extensions/score', [''])[0]
                            time = pd.to_datetime(row.get('timestamp', ''), errors='coerce')  # Safely parse timestamp
                            verb = safe_json_load(row.get('verb', '{}')).get('display', {}).get('en-US', '')
                            
                            if level and level.startswith('['):
                                # Clean or format the level name if necessary
                                match = re.search(r'\[en\](.*?)\[/en\]', level)
                                if match:
                                    level = match.group(1).strip()

                            if verb.lower() in ['launched', 'completed', 'exited'] :
                                extracted_rows.append({
                                    'actor': actor,
                                    'scenario': scenario,
                                    'level': level,
                                    'score': score,
                                    'time': time,
                                    'verb': verb
                                })
                        except Exception as e:
                            print(f"Unexpected error processing row in file {file_name}: {e}")
            except Exception as e:
                print(f"Error processing file {file_name}: {e}")
    
    # Group data by actor and write to individual CSV files
    grouped_data = pd.DataFrame(extracted_rows).sort_values(by='time')

    for actor, group in grouped_data.groupby('actor'):
        actor_filename = f"{actor.replace(' ', '_').replace('/', '_')}.csv"  # Replace spaces and slashes in filenames
        actor_file_path = os.path.join(output_folder, actor_filename)
        
        group.to_csv(actor_file_path, index=False, columns=fieldnames)
        #print(f"Data for actor '{actor}' saved to {actor_file_path}.")

    print(f"Data processing complete. All data saved to {output_folder}.")


# Run the script
# input_folder = 'traces' 
# output_folder = 'processed_data'  
# process_csv_files(input_folder, output_folder)

