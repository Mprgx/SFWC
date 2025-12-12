# import pytest
# import requests

# VEHICLE_LICENSE = "76-KQQ-7"


# def test_post_vehicle_license_status_unauthorized(user_session):
#     url = user_session['url'] + 'vehicles/' + VEHICLE_LICENSE
#     response = requests.put(url, headers={})
#     status_code = response.status_code

#     assert status_code == 401


# def test_post_vehicle_license_status_authorized(user_session):
#     url = user_session['url'] + 'vehicles/' + VEHICLE_LICENSE
#     response = requests.put(
#         url, headers={"Authorization": user_session['session_token']})
#     status_code = response.status_code
#     assert status_code == 200


# def test_post_vehicle_license_responsebody(user_session):
#     url = user_session['url'] + 'vehicles/' + VEHICLE_LICENSE
#     response = requests.put(
#         url, headers={"Authorization": user_session['session_token']})
#     assert response.status_code == 200
#     expected = {
#         "message": f"Vehicle with licenseplate: {VEHICLE_LICENSE} succesfully updated"}
#     assert response.json() == expected


# def test_post_vehicle_license_invalid_license_message(user_session):
#     invalid_license = "INVALID123"
#     url = user_session['url'] + 'vehicles/' + invalid_license
#     response = requests.put(
#         url, headers={"Authorization": user_session['session_token']})
#     assert response.status_code == 400
#     expected = {
#         "message": f"Vehicle with licenseplate: {invalid_license} does not exist"}
#     assert response.json() == expected


# def test_post_vehicle_license_user_doesnt_have_vehicle_to_his_acccount(user_session):
#     license_not_in_account = "84-WXD-8"
#     url = user_session['url'] + 'vehicles/' + license_not_in_account
#     response = requests.put(
#         url, headers={"Authorization": user_session['session_token']})
#     assert response.status_code == 400


# def test_post_vehicle_license_user_doesnt_have_vehicle_to_his_acccount_message(user_session):
#     license_not_in_account = "84-WXD-8"
#     url = user_session['url'] + 'vehicles/' + license_not_in_account
#     response = requests.put(
#         url, headers={"Authorization": user_session['session_token']})
#     assert response.status_code == 400
#     expected = {
#         "message": f"Vehicle with licenseplate: {license_not_in_account} does not belong to user with id: 8592"}
#     assert response.json() == expected
