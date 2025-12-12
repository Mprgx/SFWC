# import requests

# # Checks if you can access a session as a admin without a token


# def test_sessions_unauthorized(admin_session):
#     url = admin_session['url'] + '/parking-lots/1/sessions/1'

#     response = requests.get(url)
#     assert response.status_code == 403
#     assert response.text == "Unauthorized: Invalid or missing session token"

# # Checks if you can access a session as a user without a token


# def test_sessions_unauthorized(user_session):
#     url = user_session['url'] + '/parking-lots/1/sessions/1'

#     response = requests.get(url)
#     assert response.status_code == 403
#     assert response.text == "Unauthorized: Invalid or missing session token"

# # Checks if a user has access to a session that isn't theirs


# def test_sessions_forbidden(user_session):
#     url = user_session['url'] + '/parking-lots/1/sessions/9999'

#     response = requests.get(
#         url,
#         headers={"Authorization": user_session['session_token']}
#     )
#     assert response.status_code == 403
#     assert response.text == "Access denied"

# # Checks if a user can access a specific session


# def test_get_specific_session_as_user(user_session):
#     url = user_session['url'] + '/parking-lots/1/sessions/1'

#     response = requests.get(
#         url,
#         headers={"Authorization": user_session['session_token']}
#     )
#     assert response.status_code == 200
#     assert isinstance(response.json(), dict)

# # Checks if a admin can access a specific session


# def test_get_specific_session_as_admin(admin_session):
#     url = admin_session['url'] + '/parking-lots/1/sessions/1'

#     response = requests.get(
#         url,
#         headers={"Authorization": admin_session}
#     )
#     assert response.status_code == 200
#     assert isinstance(response.json(), dict)
