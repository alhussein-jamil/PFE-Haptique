import cv2
import redis
from flask import Flask, render_template, request
from threading import Thread
import requests
import numpy as np
from PIL import Image
from io import BytesIO
import pandas as pd
import datetime  # Import datetime module for timestamp

app = Flask(__name__)
redis_client = redis.StrictRedis(host='10.245.239.12', port=6379, db=0)

# Read parameter choices from CSV file
parameter_choices = pd.read_csv('Data_participant_Incongruency.csv', sep=";")
# Convert numeric values to strings
parameter_choices = parameter_choices.applymap(str)

parameter_names = list(parameter_choices.columns)
selected_params = {param: list(set(parameter_choices[param])) for param in parameter_names}
selected_params['SceneType'] = ['robot', 'haptic']
parameter_names.append('SceneType')
# Create a list to store received parameters
received_params = []

def oculus_stream():
    while True:
        try:
            # Fetch the Oculus stream
            response = requests.get('https://oculus.com/casting')
            if response.status_code == 200:
                # Extract the image from the response
                img = Image.open(BytesIO(response.content))
                frame = cv2.cvtColor(np.array(img), cv2.COLOR_RGB2BGR)

                # Display the frame
                cv2.imshow('Oculus Stream', frame)
                cv2.waitKey(1)
        except Exception as e:
            print(f"Error fetching Oculus stream: {e}")

def save_to_txt(param_data):
    # Save received parameters to a text file with timestamp
    timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f"received_params_{timestamp}.txt"

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

        # Save received parameters to a text file
        save_to_txt(updated_params)

    return "Parameters updated successfully"

if __name__ == '__main__':
    # Start the Oculus stream thread
    oculus_thread = Thread(target=oculus_stream)
    oculus_thread.start()

    # Run the Flask web server
    app.run(debug=True)
