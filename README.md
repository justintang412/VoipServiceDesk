# VoipServiceDesk
Desktop applicaiton to manage voip phones and calls with WPF (Windows Presentation Foundation)

## What is it?

It is developed to mananage VOIP phones connected to IPPBX, for which please refer http://www.yeastar.cn/yeastar-s100/ for more details. It monitors calls and records them. It's features are as following (and more):

- make a phone call
- monitor current calls
- disconnect parties of calls 
- pick up
- transfer
- hangup
- ring
- spy on ongoing calls
- start a meeting
- manage attenders of a meeting
- broadcast
- call one by one automatically
- manage contacts
- make alarm over messages (a message trigers alarm ends up a phone call to someone)

It uses `WPF` as the presentation layer, and Mongodb as back of data store. It uses `rest` api to communicate to the IPPBX, produced by provider mentioned above.

PS: 
- Login:
![login](https://user-images.githubusercontent.com/53476518/110637084-07fe5100-817b-11eb-8829-123bbcd4b666.png)

- incoming call
![ring](https://user-images.githubusercontent.com/53476518/110637153-18aec700-817b-11eb-8bbd-99c5c7b9d288.png)

- when call happened
![when call happened](https://user-images.githubusercontent.com/53476518/110637189-21070200-817b-11eb-8d41-e4ed7024b37d.png)

