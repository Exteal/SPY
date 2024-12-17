import os
import requests
from requests.auth import HTTPBasicAuth
import csv
import json

# LRS API credentials and URL

username = "9fe9fa9a494f2b34b3cf355dcf20219d7be35b14" 
password = "b547a66817be9c2dbad2a5f583e704397c9db809" 
base_url = "https://lrsels.lip6.fr/data/xAPI/statements"

# Output folder for CSV files
output_folder = "traces"

# Ensure output folder exists
if not os.path.exists(output_folder):
    os.makedirs(output_folder)
    #print(f"Created folder: {output_folder}")

def fetch_statements(actor_name):
    """
    Fetch statements from the LRS.
    """
    headers = {
        "X-Experience-API-Version": "1.0.3",
        "Content-Type": "application/json",
    }

    agent = {"account": {"homePage": "https://www.lip6.fr/mocah/", "name": actor_name}}

    params = {
        "agent": json.dumps(agent),
        "limit": 1000, 
    }

    response = requests.get(base_url, headers=headers, params=params, auth=HTTPBasicAuth(username, password))

    if response.status_code == 200:
        data = response.json()
        statements = data.get("statements", [])
        print(f"Fetched {len(statements)} statements for actor: {actor_name}.")        
        return statements
    else:
        print(f"Error: {response.status_code} - {response.text}")
        return []

def save_statements_to_actor_csv(statements, actor_name, folder_path):
    """
    Save LRS statements to a CSV file named after the actor.
    """
    # Sanitize actor name for file naming
    sanitized_name = ''.join(c if c.isalnum() or c in (' ', '_') else '_' for c in actor_name)
    output_file = os.path.join(folder_path, f"{sanitized_name}.csv")


    # Open the file in append mode
    with open(output_file, mode='w', newline='', encoding='utf-8') as csvfile:
        fieldnames = [
            "id", "timestamp", "stored", "actor", 
            "verb", "object", "result", "authority", "context"
        ]
        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
        writer.writeheader()

        # Normalize the JSON structure into flat rows for CSV
        for statement in statements:
            writer.writerow({
                "id": statement.get("id", ""),
                "timestamp": statement.get("timestamp", ""),
                "stored": statement.get("stored", ""),
                "actor": statement.get("actor", {}),
                "verb": statement.get("verb", {}),
                "object": statement.get("object", {}),
                "result": statement.get("result", ""),
                "authority": statement.get("authority", {}),
                "context": statement.get("context", {}),
            })

    print(f"Saved {len(statements)} statements to {output_file}.")


def request_data(output_folder):
    """
    Fetch corresponding data, and save to CSV.
    """
    actor_name = input("Enter the session ID: ").strip()
    
    statements = fetch_statements(actor_name)
    if statements:
        save_statements_to_actor_csv(statements, actor_name, output_folder)

# Run the data request
request_data(output_folder)
