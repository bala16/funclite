FROM ruby:2.4

COPY ./app /app
COPY ./userFunc /userFunc
COPY init_container.sh /bin/
COPY sshd_config /etc/ssh/

EXPOSE 4567 2222

RUN apt-get update -qq \
    && apt-get install -y openssh-server dos2unix --no-install-recommends \
    && echo "root:Docker!" | chpasswd \
    && gem install sinatra sinatra-contrib \
    && dos2unix /bin/init_container.sh
CMD ["/bin/init_container.sh"]
