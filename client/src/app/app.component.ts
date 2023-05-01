import { Component, OnInit } from '@angular/core';
import { AccountService } from './_services/account.service';
import { User } from './_models/user';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  
  title = 'Dating app';
  
  constructor(private accountService: AccountService) {}
  
  ngOnInit(): void {
    //Set our user object if we have something in local storage
    this.setCurrentUser();
  }  

  setCurrentUser(){
    const userString = localStorage.getItem('user');
    if (!userString) return;
    const user: User = JSON.parse(userString);
    this.accountService.serCurrentUser(user);
  }

}
