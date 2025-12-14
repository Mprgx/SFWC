# import pytest
# import requests


# def test_parking_lots_authorized(user_session):
#     url = user_session['url'] + 'parking-lots/'
#     response = requests.get(
#         url, headers={"Authorization": user_session['session_token']})

#     assert response.status_code == 200
#     assert list(response.json().values())[0] == {
#         "id": "1",
#         "name": "Bedrijventerrein Almere Parkeergarage",
#         "location": "Industrial Zone",
#         "address": "Schanssingel 337, 2421 BS Almere",
#         "capacity": 335,
#         "reserved": 77,
#         "tariff": 1.9,
#         "daytariff": 11,
#         "created_at": "2020-03-25",
#         "coordinates": {"lat": 52.3133, "lng": 5.2234}
#     }


# def test_parking_lots_unauthorized(user_session):
#     url = user_session['url'] + 'parking-lots/'
#     response = requests.get(url, headers={})

#     assert response.status_code == 401


# def test_parking_lots_lid_authorized(user_session):
#     url = user_session['url'] + 'parking-lots/1485'
#     response = requests.get(
#         url, headers={"Authorization": user_session['session_token']})
#     assert response.status_code == 200
#     assert response.json() == {
#         "id": "1485",
#         "name": "Doetinchem Sportcomplex Parkeergarage",
#         "location": "Sports Stadium",
#         "address": "Havenweg 631, 4858 BR Doetinchem",
#         "capacity": 1820,
#         "reserved": 387,
#         "tariff": 5.6,
#         "daytariff": 14,
#         "created_at": "2020-12-11",
#         "coordinates": {
#             "lat": 51.9899,
#             "lng": 6.2475
#         }
#     }


# def test_parking_lots_lid_unauthorized(user_session):
#     url = user_session['url'] + 'parking-lots/1485'
#     response = requests.get(url, headers={})
#     assert response.status_code == 401

# def test_get_all_parking_lots_as_admin(login_as_admin):
#     url = login_as_admin['url'] + '/parking-lots'
#     response = requests.get(url, headers={"Authorization": login_as_admin['session_token']})
#     assert response.status_code == 200
#     assert response.headers["Content-Type"] == "application/json"
#     data = response.json()
#     assert isinstance(data, dict)
#     assert len(data) > 0

# def test_get_all_parking_lots_as_user(login_as_user):
#     url = login_as_user['url'] + '/parking-lots'
#     response = requests.get(url, headers={"Authorization": login_as_user['session_token']})
#     assert response.status_code == 200
#     assert isinstance(response.json(), dict)

# def test_get_all_parking_lots_no_token(login_as_admin):
#     url = login_as_admin['url'] + '/parking-lots'
#     response = requests.get(url)
#     assert response.status_code == 401
#     assert "Unauthorized" in response.text

# def test_get_all_parking_lots_invalid_token(login_as_admin):
#     url = login_as_admin['url'] + '/parking-lots'
#     response = requests.get(url, headers={"Authorization": "invalid-token"})
#     assert response.status_code == 401
#     assert "Unauthorized" in response.text

# def test_get_all_parking_lots_response_structure(login_as_admin):
#     url = login_as_admin['url'] + '/parking-lots'
#     response = requests.get(url, headers={"Authorization": login_as_admin['session_token']})
#     data = response.json()
#     for lot_id, lot in data.items():
#         assert "name" in lot
#         assert "location" in lot
