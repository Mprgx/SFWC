import requests

# Checks if a admin token is not valid


def test_logout(admin_session):
    url = admin_session['url'] + '/logout'

    response = requests.get(url)

    assert response.status_code == 403
    assert response.text == "Invalid session token"

# Checks if a user token is not valid


def test_logout(user_session):
    url = user_session['url'] + '/logout'

    response = requests.get(url)

    assert response.status_code == 403
    assert response.text == "Invalid session token"

# Checks if a admin can log out


def test_logout(admin_session):
    url = admin_session['url'] + '/logout'

    response = requests.get(url)

    assert response.status_code == 200
    assert response.text == "User logged out"

# Checks if a user can log out


def test_logout(user_session):
    url = user_session['url'] + '/logout'

    response = requests.get(url)

    assert response.status_code == 200
    assert response.text == "User logged out"
