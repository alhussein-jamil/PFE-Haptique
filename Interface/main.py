
import redis
from flask import Flask, render_template, request
from threading import Thread
import pandas as pd
import datetime  # Import datetime module for timestamp
import subprocess
import os
import csv
import webbrowser
from flask import jsonify
 
os.makedirs('./feedback_data', exist_ok=True)
# Initialize participant_id from the latest saved data
participant_id = 0
for filename in os.listdir('./feedback_data'):
    if filename.endswith(".csv"):
        participant_id = max(participant_id, int(filename.split('_')[2].split('.')[0]))



#run the c# client using dotnet run
import os

subprocess.Popen(["dotnet", "run", "--project", "./UDP/"])
app = Flask(__name__)
redis_client = redis.StrictRedis(host='localhost', port=6379, db=0)

# Read parameter choices from CSV file
parameter_choices = pd.read_csv('Data_participant_Incongruency.csv', sep=";")
# Convert numeric values to strings
parameter_choices = parameter_choices.applymap(str)

parameter_names = list(parameter_choices.columns)
selected_params = {param: list(set(parameter_choices[param])) for param in parameter_names}

selected_params['SceneType'] = ['robot', 'haptic']
selected_params['Side'] = ['left', 'right']

parameter_names.append('SceneType')
parameter_names.append('Side')
# Add a dictionary to store current parameter values
current_values = {param: selected_params[param][0] for param in parameter_names}

# Create a list to store received parameters
received_params = []
# Add this route to your Flask app
@app.route('/send_to_redis', methods=['POST'])
def send_to_redis():

    value_to_send = request.form.get('value')
    if value_to_send:
        redis_client.publish('caresse', value_to_send)
        return f"Value '{value_to_send}' sent to Redis channel 'caresse'"
    else:
        return "No value provided to send to Redis channel 'caresse'"
def update_selected_params(updated_params):
    global selected_params
    global current_values

    for param, value in updated_params.items():
        selected_params[param] = list(set(parameter_choices[param]))  # Reset options
        current_values[param] = value  # Update current value
        selected_params[param].insert(0, value)  # Insert the updated value at the beginning

@app.route('/')
def index():
    return render_template('index_ajax.html', parameters=selected_params, current_values=current_values, participant_id=participant_id)
@app.route('/update_participant_id', methods=['POST'])
def update_participant_id():
    global participant_id
    new_participant_id = request.form.get('participant_id')
    
    if new_participant_id:
        participant_id = int(new_participant_id)
        return jsonify(participant_id=participant_id)  # Return the updated participant ID as JSON
    else:
        return "No participant ID provided"

@app.route('/update_params', methods=['POST'])
def update_params():
    global selected_params
    global received_params

    # Get the current values
    current_params = selected_params.copy()

    # Update the selected_params with the values from the form
    for param in parameter_names:
        selected_params[param] = request.form[param]

    # Find the updated parameters
    updated_params = {param: selected_params[param] for param in parameter_names if selected_params[param] != current_params[param]}

    # Publish only the updated parameters to the Redis channel
    if updated_params:
        for param in updated_params:
            redis_client.publish('game_parameters', f"{param};{updated_params[param]}")

        # Save received parameters to the list
        received_params.append(updated_params)

    return "Parameters updated successfully"


def listen_and_save():
    for message in pubsub.listen():
        if message['type'] == 'message':
            if message['channel'] == b'feedback':
                # Extract parameter values from the feedback message
                feedback_data = message['data'].decode('utf-8').split(';')
                pleasantness_value = feedback_data[0].split(' : ')[1]
                intensity_value = feedback_data[1].split(' : ')[1]


                # Create a dictionary with parameter names and their values
                feedback_params = {'pleasantness': pleasantness_value, 'intensity': intensity_value}
                print(feedback_params)
                # Save the feedback parameters to a CSV file
                save_feedback_to_csv(feedback_params, current_values)


            elif message['channel'] == b'game_parameters':
                # Extract parameter values from the game parameters message
                game_params = message['data'].decode('utf-8').split(';')
                game_params_dict = {game_params[i]: game_params[i+1] for i in range(0, len(game_params), 2)}

                # Update the selected parameters
                update_selected_params(game_params_dict)

def save_feedback_to_csv(feedback_params, sec_param):
    timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f"feedback_data/participant_id_{str(participant_id)}.csv"

    # Check if the CSV file already exists
    file_exists = os.path.isfile(filename)

    # Prepare the data to be saved
    to_save = {"timestamp": timestamp}
    to_save.update(sec_param)
    to_save.update(feedback_params)

    with open(filename, 'a', newline='') as csvfile:
        fieldnames = list(to_save.keys())
        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)

        # Write the header only if the file is empty
        if not file_exists:
            writer.writeheader()

        # Write the data
        writer.writerow(to_save)


if __name__ == '__main__':
    pubsub = redis_client.pubsub()
    pubsub.subscribe("feedback")
    pubsub.subscribe("game_parameters")
    from queue import Queue

    # ... (previous code)


    # Create a new thread for listening and DebugLog saving
    listen_thread = Thread(target=listen_and_save)
    listen_thread.daemon = True  # Set the thread as daemon
    listen_thread.start()
    
    # open the web browser to the c# client
    webbrowser.open('http://localhost:5000/')
    # Run the Flask web server
    app.run(debug=True)
