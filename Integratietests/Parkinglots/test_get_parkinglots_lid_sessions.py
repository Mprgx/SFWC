import requests

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

# Checks if a admin has access to all sessions


def test_get_sessions_as_admin(admin_session):
    url = admin_session['url'] + '/parking-lots/1/sessions'

    response = requests.get(
        url,
        headers={"Authorization": admin_session}
    )
    assert response.status_code == 200
    assert response.headers["Content-Type"] == "application/json"
    assert isinstance(response.json(), list)

# Checks if a user can get all of their sessions


def test_get_own_sessions_as_user(user_session):
    url = user_session['url'] + '/parking-lots/1/sessions'

    response = requests.get(
        url,
        headers={"Authorization": user_session['session_token']}
    )
    assert response.status_code == 200
    sessions = response.json()
    for s in sessions:
        assert s['user'] == "User"
