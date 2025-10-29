sessions = {}


def add_session(token, username, role):
    sessions[token] = {"user": username, "role": role}


def remove_session(token):
    return sessions.pop(token, None)


def get_session(token):
    return sessions.get(token)
