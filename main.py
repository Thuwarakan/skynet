
from pyngrok import ngrok 

#a = raw_input("python installed dir ? ")
ngrok.set_auth_token("2EyVa3EBR4fJfB5gOjyHd4VnlZ0_43D6JtRgetuchCLUw9iF5")

ssh_tunnel = ngrok.connect(input('PORT :'), "tcp")

ngrok_process = ngrok.get_ngrok_process()
#API

for t in ngrok.get_tunnels():
    print(t)

try:
    # Block until CTRL-C or some other terminating event
    ngrok_process.proc.wait()
except KeyboardInterrupt:
    print(" Shutting down server.")

    ngrok.kill()
