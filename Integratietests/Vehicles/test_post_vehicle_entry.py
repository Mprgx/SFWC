# import pytest
# import requests

# PARKING_LOT_ID = "1"


# def test_post_vehicle_entry_status_unauthorized(user_session):
#     url = user_session['url'] + 'vehicles/' + PARKING_LOT_ID + '/entry'
#     response = requests.post(url, headers={})
#     status_code = response.status_code

#     assert status_code == 401


# def test_post_vehicle_entry_status_authorized(user_session):
#     url = user_session['url'] + 'vehicles/' + PARKING_LOT_ID + '/entry'
#     response = requests.post(
#         url, headers={"Authorization": user_session['session_token']})
#     status_code = response.status_code

#     assert status_code == 200


# def test_post_vehicle_entry_responsebody(user_session):
#     url = user_session['url'] + 'vehicles/' + PARKING_LOT_ID + '/entry'
#     response = requests.post(
#         url, headers={"Authorization": user_session['session_token']})

#     assert response.status_code == 200
#     expected = {
#         "message": f"Vehicle entry started for parking lot :{PARKING_LOT_ID}"}
#     assert response.json() == expected


# def test_post_vehicle_entry_invalid_parkinglot_message(user_session):
#     invalid_parking_lot_id = "2220000"
#     url = user_session['url'] + 'vehicles/' + invalid_parking_lot_id + '/entry'
#     response = requests.post(
#         url, headers={"Authorization": user_session['session_token']})
#     assert response.status_code == 400
#     expected = {
#         "message": f"Parking lot with id: {invalid_parking_lot_id} does not exist"}
#     assert response.json() == expected


# def test_post_vehicle_entry_nonnumeric_input(user_session):
#     non_numeric_parking_lot_id = "abc"
#     url = user_session['url'] + 'vehicles/' + \
#         non_numeric_parking_lot_id + '/entry'
#     response = requests.post(
#         url, headers={"Authorization": user_session['session_token']})
#     assert response.status_code == 400
#     expected = {
#         "message": f"Invalid parking lot ID: {non_numeric_parking_lot_id}. It must be a numeric value."}
#     assert response.json() == expected
