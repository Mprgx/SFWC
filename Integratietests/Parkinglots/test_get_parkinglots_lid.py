import requests

# Checks if a parking lot isn't found


def test_parking_lot_not_found(admin_session):
    url = admin_session['url'] + '/parking-lots/0'

    response = requests.get(
        url,
        headers={"Authorization": admin_session['session_token']})

    assert response.status_code == 404
    assert response.text == "Parking lot not found"

# Checks if a parking lot is found


def test_parking_lot_found(admin_session):
    url = admin_session['url'] + '/parking-lots/1'

    response = requests.get(
        url,
        headers={"Authorization": admin_session['session_token']})

    assert response.status_code == 200
    assert response.headers["Content-Type"] == "application/json"

# Checks if you can access a session as a admin without a token


def test_sessions_unauthorized(admin_session):
    url = admin_session['url'] + '/parking-lots/1/sessions'

    response = requests.get(url)
    assert response.status_code == 403
    assert response.text == "Unauthorized: Invalid or missing session token"

# Checks if you can access a session as a user without a token


def test_sessions_unauthorized(user_session):
    url = user_session['url'] + '/parking-lots/1/sessions'

    response = requests.get(url)
    assert response.status_code == 403
    assert response.text == "Unauthorized: Invalid or missing session token"
