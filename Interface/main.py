
import redis
from flask import Flask, render_template, request
from threading import Thread
import pandas as pd
import datetime  # Import datetime module for timestamp
import os  # Import os module for creating directories
import subprocess
import webbrowser

# Open the webpage in the default web browser
webbrowser.open('http://127.0.0.1:5000/')

#run the c# client using dotnet run
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

def save_to_txt(param_data):
    # Save received parameters to a text file with timestamp
    timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    os.makedirs("received_params", exist_ok=True)
    filename =  f"received_params/{timestamp}.txt"

    with open(filename, 'w') as file:
        for param, value in param_data.items():
            file.write(f"{param}: {value}\n")

@app.route('/')
def index():
    return render_template('index_ajax.html', parameters=selected_params)


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
            print("Publishing to Redis: ", param, updated_params[param])
            redis_client.publish('game_parameters', f"{param};{updated_params[param]}")

        # Save received parameters to the list
        received_params.append(updated_params)

    return "Parameters updated successfully"

if __name__ == '__main__':
    pubsub = redis_client.pubsub()
    pubsub.subscribe("feedback")
    def listen_and_save():
        for message in pubsub.listen():
            if message['type'] == 'message':
                save_to_txt(message)

    # Create a new thread for listening and DebugLog saving
    listen_thread = Thread(target=listen_and_save)
    listen_thread.daemon = True  # Set the thread as daemon
    listen_thread.start()
    # Run the Flask web server
    app.run(debug=True)
