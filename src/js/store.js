﻿import { createStore } from "vuex";

const store = createStore({
    state: {
        user: null,
    },
    mutations: {
        setUser(state, user) {
            state.user = user;
        },
    },
    getters: {
        isLoggedIn(state) {
            return state.user !== null;
        },
    },
});

export default store;